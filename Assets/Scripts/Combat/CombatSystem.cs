using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    [Header("Combat Settings")]
    public float impactForceMultiplier = 2f;
    public float rpmLossMultiplier = 1f;

    [Header("Particle Effects")]
    public ParticleSystem globalCollisionEffect; // Efecto global de colisiones
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
        // Calcular fuerzas de impacto
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 blade1Velocity = blade1.GetComponent<Rigidbody>().linearVelocity;
        Vector3 blade2Velocity = blade2.GetComponent<Rigidbody>().linearVelocity;

        // Determinar qui�n tiene m�s velocidad
        float blade1Speed = blade1Velocity.magnitude;
        float blade2Speed = blade2Velocity.magnitude;

        BeyBladeController stronger = blade1Speed > blade2Speed ? blade1 : blade2;
        BeyBladeController weaker = blade1Speed > blade2Speed ? blade2 : blade1;

        // Calcular daño e intensidad
        float speedDifference = Mathf.Abs(blade1Speed - blade2Speed);
        float damage = speedDifference * rpmLossMultiplier;
        float impactIntensity = speedDifference / 10f; // Normalizar para efectos

        // Aplicar daño al más débil
        weaker.ModifyRPM(-damage);

        // Efectos visuales y de partículas
        PlayCollisionEffects(impactPoint, collision.contacts[0].normal, impactIntensity);

        // Efectos específicos en cada BeyBlade
        blade1.GetParticleManager()?.PlayCollisionSparks(impactPoint, impactIntensity);
        blade2.GetParticleManager()?.PlayCollisionSparks(impactPoint, impactIntensity);

        // Audio
        PlayCollisionAudio(impactIntensity);

        Debug.Log($"Collision: {stronger.name} vs {weaker.name} - Damage: {damage} - Intensity: {impactIntensity}");

    }


    private void PlayCollisionEffects(Vector3 position, Vector3 normal, float intensity)
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