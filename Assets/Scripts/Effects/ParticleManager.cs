using UnityEngine;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    [Header("Collision Particles")]
    public ParticleSystem collisionSparks;
    public ParticleSystem trailParticles;

    [Header("Collision Settings")]
    public float minIntensityForSparks = 2f;
    public float maxSparkIntensity = 10f;
    public int minSparkCount = 10;
    public int maxSparkCount = 50;

    [Header("Colors by BeyBlade Type")]
    public Color attackSparks = Color.red;
    public Color defenseSparks = Color.blue;
    public Color agilitySparks = Color.yellow;
    public Color balancedSparks = Color.white;

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
        if (collisionSparks != null)
        {
            emissionModule = collisionSparks.emission;
            mainModule = collisionSparks.main;

            // Configurar color según el tipo de BeyBlade
            if (owner != null && owner.stats != null)
            {
                Color sparkColor = GetSparkColorByType(owner.stats.type);
                mainModule.startColor = sparkColor;
            }

            // Desactivar emisión automática
            emissionModule.enabled = false;
        }
    }

    public void PlayCollisionSparks(Vector3 contactPoint, float intensity)
    {
        if (collisionSparks == null || intensity < minIntensityForSparks)
            return;

        // Posicionar el sistema de partículas en el punto de contacto
        collisionSparks.transform.position = contactPoint;

        // Configurar intensidad basada en la velocidad del impacto
        float normalizedIntensity = Mathf.Clamp01(intensity / maxSparkIntensity);

        // Calcular número de partículas
        int sparkCount = Mathf.RoundToInt(Mathf.Lerp(minSparkCount, maxSparkCount, normalizedIntensity));

        // Configurar velocidad y tamaño de partículas
        mainModule.startSpeed = Mathf.Lerp(3f, 8f, normalizedIntensity);
        mainModule.startSize = Mathf.Lerp(0.05f, 0.15f, normalizedIntensity);

        // Emitir partículas
        collisionSparks.Emit(sparkCount);

        Debug.Log($"Collision sparks: {sparkCount} particles at intensity {intensity:F2}");
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
}