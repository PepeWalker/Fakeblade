using UnityEngine;

[CreateAssetMenu(fileName = "ParticleEffectPreset", menuName = "BeyBlade/Particle Effect Preset")]
public class ParticleEffectPreset : ScriptableObject
{
    [Header("Trail Settings")]
    public Color trailColor = Color.white;
    public float trailWidth = 0.2f;
    public float trailLifetime = 1f;
    public int trailParticleCount = 50;

    [Header("Spark Settings")]
    public Color sparkColor = Color.yellow;
    public int sparkBurstCount = 25;
    public float sparkSpeed = 8f;
    public float sparkLifetime = 0.5f;

    [Header("Special Effect")]
    public string specialEffectName = "Default";
    public Color specialColor = Color.red;
    public float specialIntensity = 1f;
    public float specialDuration = 3f;

    public void ApplyToParticleManager(ParticleManager manager)
    {
        // Aplicar configuraciones al ParticleManager
        
    }
}