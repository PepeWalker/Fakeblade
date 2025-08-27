using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    [Header("Combat Settings")]
    public float impactForceMultiplier = 15f; // Aumento para más recoil
    public float rpmLossMultiplier = 1f;
    public float recoilForceMultiplier = 8f; // Multiplicador específico para recoil

    [Header("Recoil Settings")]
    public float minRecoilForce = 200f;
    public float maxRecoilForce = 800f;
    public float massInfluenceOnRecoil = 0.3f; // Cómo afecta la masa al recoil
    public float verticalRecoilFactor = 0.2f; // Pequeño impulso hacia arriba

    [Header("Particle Effects")]
    public ParticleSystem globalCollisionEffect;
    public AudioClip[] collisionSounds;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void HandleCollision(BeyBladeController blade1, BeyBladeController blade2, Collision collision)
    {
        if (blade1 == null || blade2 == null) return;

        // Obtener rigidbodies
        Rigidbody rb1 = blade1.GetComponent<Rigidbody>();
        Rigidbody rb2 = blade2.GetComponent<Rigidbody>();

        if (rb1 == null || rb2 == null) return;

        // Calcular velocidades y punto de impacto
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 blade1Velocity = rb1.linearVelocity;
        Vector3 blade2Velocity = rb2.linearVelocity;

        // Velocidades relativas
        float blade1Speed = blade1Velocity.magnitude;
        float blade2Speed = blade2Velocity.magnitude;
        float relativeSpeed = (blade1Velocity - blade2Velocity).magnitude;

        // Determinar quién es más fuerte basado en velocidad y masa
        float blade1Power = blade1Speed * rb1.mass;
        float blade2Power = blade2Speed * rb2.mass;

        BeyBladeController stronger = blade1Power > blade2Power ? blade1 : blade2;
        BeyBladeController weaker = blade1Power > blade2Power ? blade2 : blade1;
        Rigidbody strongerRb = blade1Power > blade2Power ? rb1 : rb2;
        Rigidbody weakerRb = blade1Power > blade2Power ? rb2 : rb1;

        // Calcular daño basado en diferencia de poder
        float powerDifference = Mathf.Abs(blade1Power - blade2Power);
        float damage = (powerDifference * rpmLossMultiplier) / 10f; // Normalizar
        float impactIntensity = relativeSpeed / 5f; // Para efectos visuales

        // Aplicar daño al más débil
        weaker.ModifyRPM(-damage);

        // ===== APLICAR RECOIL FUERTE =====
        ApplyRecoilForces(strongerRb, weakerRb, impactPoint, relativeSpeed);

        // Efectos visuales y partículas
        PlayCollisionEffects(impactPoint, collision.contacts[0].normal, impactIntensity, blade1, blade2);

        // Audio
        PlayCollisionAudio(impactIntensity);

        Debug.Log($"Collision: {stronger.name} (Power: {(blade1Power > blade2Power ? blade1Power : blade2Power):F1}) " +
          $"vs {weaker.name} (Power: {(blade1Power > blade2Power ? blade2Power : blade1Power):F1}) " +
          $"- Damage: {damage:F1} - Intensity: {impactIntensity:F1}");
    }

    private void ApplyRecoilForces(Rigidbody strongerRb, Rigidbody weakerRb, Vector3 impactPoint, float relativeSpeed)
    {
        // Calcular dirección del recoil
        Vector3 collisionDirection = (weakerRb.position - strongerRb.position).normalized;

        // Asegurar que la dirección sea horizontal principalmente
        collisionDirection.y = 0;
        collisionDirection.Normalize();

        // Calcular fuerza de recoil basada en velocidad relativa y masas
        float baseRecoilForce = Mathf.Lerp(minRecoilForce, maxRecoilForce, relativeSpeed / 20f);

        // El más ligero recibe más recoil
        float weakerRecoilForce = baseRecoilForce * recoilForceMultiplier;
        float strongerRecoilForce = baseRecoilForce * recoilForceMultiplier * 0.6f; // El más fuerte también retrocede, pero menos

        // Aplicar factor de masa (objetos más pesados se mueven menos)
        weakerRecoilForce *= (1f + massInfluenceOnRecoil * (1f / weakerRb.mass));
        strongerRecoilForce *= (1f + massInfluenceOnRecoil * (1f / strongerRb.mass));

        // Vectores de recoil (direcciones opuestas)
        Vector3 weakerRecoil = collisionDirection * weakerRecoilForce;
        Vector3 strongerRecoil = -collisionDirection * strongerRecoilForce;

        // Añadir pequeño componente vertical para efecto dramático
        weakerRecoil.y = weakerRecoilForce * verticalRecoilFactor;
        strongerRecoil.y = strongerRecoilForce * verticalRecoilFactor * 0.5f;

        // Aplicar fuerzas de impulso
        weakerRb.AddForce(weakerRecoil, ForceMode.Impulse);
        strongerRb.AddForce(strongerRecoil, ForceMode.Impulse);

        Debug.Log($"Recoil Applied - Weaker: {weakerRecoilForce:F1} | Stronger: {strongerRecoilForce:F1}");
    }

    private void PlayCollisionEffects(Vector3 position, Vector3 normal, float intensity,
                                     BeyBladeController blade1, BeyBladeController blade2)
    {
        // Efecto global de colisión
        if (globalCollisionEffect != null)
        {
            globalCollisionEffect.transform.position = position;
            globalCollisionEffect.transform.LookAt(position + normal);

            var main = globalCollisionEffect.main;
            main.startSpeed = intensity * 8f;

            var emission = globalCollisionEffect.emission;
            int particleCount = Mathf.RoundToInt(intensity * 30f);
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(particleCount, 15, 80))
            });

            globalCollisionEffect.Play();
        }

        // Efectos específicos en cada BeyBlade - CORREGIDO
        if (blade1?.GetParticleManager() != null)
        {
            blade1.GetParticleManager().PlayCollisionSparks(position, intensity);
        }

        if (blade2?.GetParticleManager() != null)
        {
            blade2.GetParticleManager().PlayCollisionSparks(position, intensity);
        }
    }

    private void PlayCollisionAudio(float intensity)
    {
        if (collisionSounds != null && collisionSounds.Length > 0 && AudioManager.Instance != null)
        {
            int soundIndex = Mathf.RoundToInt(intensity * (collisionSounds.Length - 1));
            soundIndex = Mathf.Clamp(soundIndex, 0, collisionSounds.Length - 1);

            AudioManager.Instance.PlayCollisionSound(intensity, collisionSounds[soundIndex]);
        }
    }
}