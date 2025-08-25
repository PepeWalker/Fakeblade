using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

[CreateAssetMenu(fileName = "New BeyBlade Stats", menuName = "BeyBlade/Stats")]
public class BeyBladeStats : ScriptableObject
{
    [Header("Basic Stats")]
    public string beyBladeName = "Default";
    public BeyBladeType type = BeyBladeType.Balanced;

    [Header("Physics")]
    public float mass = 1f;
    public float maxRPM = 1000f;
    public float rpmDecayRate = 10f;
    public float spinForce = 500f;

    [Header("Combat")]
    public float attackPower = 50f;
    public float defense = 30f;
    public int maxAttackCharges = 3;
    public float chargeRecoveryTime = 2f;

    [Header("Movement")]
    public float movementSpeed = 10f;
    public float dashPower = 200f;
    public float dashCost = 50f;

    [Header("Special Power")]
    public SpecialPowerType specialType;
    public string specialName = "Default Special";

    [Header("Speed Settings")]
    public float baseMaxSpeed = 8f;        // Velocidad base según tipo
    public float speedMultiplier = 1f;     // Multiplicador por tipo de BeyBlade

    public enum BeyBladeType
    {
        Attack,
        Defense,
        Agility,
        Balanced
    }

    public enum SpecialPowerType
    {
        FireTrail,
        ElectricDash,
        StormBreaker
    }

    public void ExecuteSpecialPower(BeyBladeController controller)
    {
        switch (specialType)
        {
            case SpecialPowerType.FireTrail:
                ExecuteFireTrail(controller);
                break;
            case SpecialPowerType.ElectricDash:
                ExecuteElectricDash(controller);
                break;
            case SpecialPowerType.StormBreaker:
                ExecuteStormBreaker(controller);
                break;
        }
    }


    private void ExecuteFireTrail(BeyBladeController controller)
    {
        // Implementar rastro de fuego
        Debug.Log($"{controller.playerName} uses Fire Trail!");
    }

    private void ExecuteElectricDash(BeyBladeController controller)
    {
        // Implementar dash eléctrico
        Debug.Log($"{controller.playerName} uses Electric Dash!");
    }

    private void ExecuteStormBreaker(BeyBladeController controller)
    {
        // Implementar Storm Breaker
        Debug.Log($"{controller.playerName} uses Storm Breaker!");
    }
}