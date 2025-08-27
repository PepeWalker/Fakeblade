using UnityEngine;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    [Header("Collision Particles")]
    public ParticleSystem collisionSparks;
    public ParticleSystem trailParticles;

    [Header("Collision Settings")]
    public float minIntensityForSparks = 0.5f; 
    public float maxSparkIntensity = 10f;
    public int minSparkCount = 15;
    public int maxSparkCount = 60;

    [Header("Colors by BeyBlade Type")]
    public Color attackSparks = Color.red;
    public Color defenseSparks = Color.blue;
    public Color agilitySparks = Color.yellow;
    public Color balancedSparks = Color.white;

    [Header("Debug")]
    public bool debugParticles = true;

    private BeyBladeController owner;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;

    void Start()
    {
        owner = GetComponent<BeyBladeController>();
        InitializeParticles();
    }

    void InitializeParticles()
    {
        if (collisionSparks == null)
        {
            // Buscar en hijos si no está asignado
            collisionSparks = GetComponentInChildren<ParticleSystem>();
            if (debugParticles)
            {
                Debug.Log($"{gameObject.name}: CollisionSparks " +
                         (collisionSparks != null ? "found in children" : "NOT FOUND"));
            }
        }

        if (collisionSparks != null)
        {
            emissionModule = collisionSparks.emission;
            mainModule = collisionSparks.main;

            // Configurar colores según el tipo de BeyBlade
            if (owner != null && owner.stats != null)
            {
                Color sparkColor = GetSparkColorByType(owner.stats.type);
                mainModule.startColor = sparkColor;

                if (debugParticles)
                    Debug.Log($"{gameObject.name}: Spark color set to {sparkColor} for type {owner.stats.type}");
            }

            // Desactivar emisión automática
            emissionModule.enabled = false;

            // Configurar el sistema para emisión manual
            collisionSparks.Stop();

            if (debugParticles)
                Debug.Log($"{gameObject.name}: Particle system initialized successfully");
        }
        else
        {
            Debug.LogError($"{gameObject.name}: No ParticleSystem found for collision sparks!");
        }
    }

    public void PlayCollisionSparks(Vector3 contactPoint, float intensity)
    {
        if (collisionSparks == null)
        {
            if (debugParticles)
                Debug.LogError($"{gameObject.name}: CollisionSparks is null!");
            return;
        }

        if (intensity < minIntensityForSparks)
        {
            if (debugParticles)
                Debug.Log($"{gameObject.name}: Intensity {intensity:F2} below threshold {minIntensityForSparks}");
            return;
        }

        // Posicionar el sistema de partículas en el punto de contacto
        collisionSparks.transform.position = contactPoint;

        // Configurar intensidad basada en la velocidad del impacto
        float normalizedIntensity = Mathf.Clamp01(intensity / maxSparkIntensity);

        // Calcular número de partículas
        int sparkCount = Mathf.RoundToInt(Mathf.Lerp(minSparkCount, maxSparkCount, normalizedIntensity));

        // Configurar parámetros dinámicamente
        var main = collisionSparks.main;
        main.startSpeed = Mathf.Lerp(4f, 12f, normalizedIntensity);
        main.startSize = Mathf.Lerp(0.08f, 0.25f, normalizedIntensity);
        main.startLifetime = Mathf.Lerp(0.3f, 0.8f, normalizedIntensity);

        // Emitir partículas usando Emit()
        collisionSparks.Emit(sparkCount);

        if (debugParticles)
        {
            Debug.Log($"{gameObject.name}: Collision sparks - {sparkCount} particles at {contactPoint} " +
                     $"with intensity {intensity:F2}");
        }
    }

    public void PlayTrailEffect(bool enable)
    {
        if (trailParticles != null)
        {
            if (enable)
                trailParticles.Play();
            else
                trailParticles.Stop();
        }
    }

    private Color GetSparkColorByType(BeyBladeStats.BeyBladeType type)
    {
        switch (type)
        {
            case BeyBladeStats.BeyBladeType.Attack:
                return attackSparks;
            case BeyBladeStats.BeyBladeType.Defense:
                return defenseSparks;
            case BeyBladeStats.BeyBladeType.Agility:
                return agilitySparks;
            case BeyBladeStats.BeyBladeType.Balanced:
                return balancedSparks;
            default:
                return Color.white;
        }
    }

    // Método para cambiar color dinámicamente (útil para power-ups)
    public void SetSparkColor(Color newColor)
    {
        if (collisionSparks != null)
        {
            var main = collisionSparks.main;
            main.startColor = newColor;
        }
    }


    [ContextMenu("Test Particles")]
    public void TestParticles()
    {
        PlayCollisionSparks(transform.position, 5f);
    }
}