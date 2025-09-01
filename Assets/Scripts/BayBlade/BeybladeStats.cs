using UnityEngine;

[CreateAssetMenu(fileName = "New BeyBlade", menuName = "BeyBlade/BeyBlade Stats")]
public class BeyBladeStats : ScriptableObject
{
    [Header("Basic Info")]
    public string beyBladeName = "Default BeyBlade";
    public BeyBladeType type = BeyBladeType.Balanced;

    [Header("Physical Properties")]
    [Range(0.5f, 5f)] public float mass = 2f;
    [Range(1000, 5000)] public float maxRPM = 3000f;
    [Range(0f, 50f)] public float rpmDecayRate = 5f;
    [Range(1000, 6000)] public float spinForce = 3000f;

    [Header("Combat Stats")]
    [Range(10, 200)] public float attackPower = 50f;
    [Range(10, 100)] public float defense = 30f;

    [Header("Attack System")]
    [Range(1, 6)] public int maxAttackCharges = 3;
    [Range(0.5f, 5f)] public float chargeRecoveryTime = 2f;

    [Header("Movement")]
    [Range(2f, 12f)] public float movementSpeed = 4f;
    [Range(50, 500)] public float dashPower = 200f;
    [Range(20, 100)] public float dashCost = 50f;

    [Header("Special Power")]
    public SpecialType specialType = SpecialType.FireTrail;
    public string specialName = "Fire Trail";

    [Header("Audio")]
    public AudioClip spinSound;
    public AudioClip attackSound;
    public AudioClip dashSound;
    public AudioClip specialSound;

    [Header("Visual Effects")]
    public ParticleEffectPreset particleEffects;
    public Material trailMaterial;
    public Color bladeColor = Color.white;

    // Method to validate stats based on type
    private void OnValidate()
    {
        ValidateStatsByType();
    }

    private void ValidateStatsByType()
    {
        switch (type)
        {
            case BeyBladeType.Attack:
                // Attack types should have high attack, medium speed, medium defense
                if (attackPower < 60f) attackPower = 70f;
                if (movementSpeed < 4f) movementSpeed = 5f;
                if (defense > 40f) defense = 35f;
                if (maxAttackCharges < 3) maxAttackCharges = 4;
                break;

            case BeyBladeType.Defense:
                // Defense types should have high defense, high mass, low speed
                if (defense < 50f) defense = 60f;
                if (mass < 3f) mass = 3.5f;
                if (movementSpeed > 6f) movementSpeed = 4f;
                if (attackPower > 60f) attackPower = 40f;
                break;

            case BeyBladeType.Agility:
                // Agility types should have high speed, low mass, many charges
                if (movementSpeed < 6f) movementSpeed = 8f;
                if (mass > 2f) mass = 1.5f;
                if (maxAttackCharges < 4) maxAttackCharges = 5;
                if (defense > 35f) defense = 25f;
                break;

            case BeyBladeType.Balanced:
                // Balanced types should have medium everything
                attackPower = Mathf.Clamp(attackPower, 45f, 65f);
                defense = Mathf.Clamp(defense, 30f, 45f);
                movementSpeed = Mathf.Clamp(movementSpeed, 4f, 6f);
                mass = Mathf.Clamp(mass, 1.8f, 2.5f);
                maxAttackCharges = 3;
                break;
        }
    }

    // Get stat summary for UI
    public string GetStatSummary()
    {
        return $"{beyBladeName}\nType: {type}\nAttack: {attackPower:F0}\nDefense: {defense:F0}\nSpeed: {movementSpeed:F1}\nCharges: {maxAttackCharges}";
    }
}

public enum BeyBladeType
{
    Attack,
    Defense,
    Agility,
    Balanced
}

public enum SpecialType
{
    FireTrail,
    ElectricDash,
    StormBreaker,
    IceFreeze,
    WindBoost
}