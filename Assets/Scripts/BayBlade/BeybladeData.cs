using UnityEngine;

[CreateAssetMenu(fileName = "New BeyBlade", menuName = "BeyBlade/Beyblade Data")]
public class BeybladeData : ScriptableObject
{
    [Header("Base Stats")]
    public string beybladeName;
    public float attack;
    public float defense;
    public float stamina;
    public float weight;
    public float spinDecay;

    [Header("Special Abilities")]
    public SpecialAttack[] specialAttacks;
}
