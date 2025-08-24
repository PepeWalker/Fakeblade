using UnityEngine;
using static BeyBladeController;

[RequireComponent(typeof(Rigidbody), typeof(BeyBladePhysics), typeof(BeyBladeStats))]
public class BeyBladeController : MonoBehaviour
{
    [Header("Components")]
    public BeyBladeStats stats;
    public BeyBladePhysics physics;
    public ParticleManager particleManager;

    [Header("Player Info")]
    public int playerIndex;
    public string playerName;
    public Color playerColor = Color.white;

    [Header("Combat")]
    public float currentRPM;
    public int attackCharges;
    public float specialPower;

    [Header("Input")]
    public KeyCode attackKey = KeyCode.Space;
    public KeyCode dashKey = KeyCode.LeftShift;
    public KeyCode specialKey = KeyCode.Z;

    // Estados
    public enum BeyBladeState
    {
        Active,
        Charging,
        Attacking,
        Stunned,
        Defeated
    }

    public BeyBladeState currentState = BeyBladeState.Active;

    // Eventos
    public System.Action<BeyBladeController> OnDefeated;
    public System.Action<BeyBladeController, float> OnRPMChanged;
    public System.Action<BeyBladeController> OnAttack;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        currentRPM = stats.maxRPM;
        attackCharges = stats.maxAttackCharges;
        specialPower = 0f;

        physics.Initialize(stats);
        //particleManager.Initialize(playerColor);

        // Registrarse en el GameManager
        GameManager.Instance.RegisterPlayer(this);
    }

    private void Update()
    {
        if (currentState == BeyBladeState.Defeated) return;

        HandleInput();
        UpdateRPM();
        UpdateCharges();
        CheckDefeat();
    }

    private void HandleInput()
    {
        Vector2 movement = InputManager.Instance.GetMovementInput(playerIndex);

        if (movement != Vector2.zero)
        {
            physics.ApplyMovement(movement);
        }

        if (InputManager.Instance.GetAttackInput(playerIndex))
        {
            TryAttack();
        }

        if (InputManager.Instance.GetDashInput(playerIndex))
        {
            TryDash();
        }

        if (InputManager.Instance.GetSpecialInput(playerIndex))
        {
            TrySpecialAttack();
        }
    }

    public void TryAttack()
    {
        if (attackCharges > 0 && currentState == BeyBladeState.Active)
        {
            ExecuteAttack();
        }
    }

    public void ExecuteAttack()
    {
        currentState = BeyBladeState.Attacking;
        attackCharges--;

        physics.ExecuteAttack();
        //particleManager.PlayAttackEffect();
        //AudioManager.Instance.PlayAttackSound();

        OnAttack?.Invoke(this);

        // Volver al estado activo después del ataque
        Invoke(nameof(ResetToActiveState), 0.2f);
    }

    public void TryDash()
    {
        if (currentRPM > stats.dashCost && currentState == BeyBladeState.Active)
        {
            physics.ExecuteDash();
            ModifyRPM(-stats.dashCost);
            //particleManager.PlayDashEffect();
        }
    }

    public void TrySpecialAttack()
    {
        if (specialPower >= 100f)
        {
            stats.ExecuteSpecialPower(this);
            specialPower = 0f;
        }
    }

    public void ModifyRPM(float amount)
    {
        currentRPM = Mathf.Clamp(currentRPM + amount, 0f, stats.maxRPM);
        OnRPMChanged?.Invoke(this, currentRPM);
    }

    private void UpdateRPM()
    {
        // Pérdida natural de RPM
        float naturalDecay = stats.rpmDecayRate * Time.deltaTime;
        ModifyRPM(-naturalDecay);
    }

    private void UpdateCharges()
    {
        if (attackCharges < stats.maxAttackCharges)
        {
            // Regenerar cargas con el tiempo
            // Implementar sistema de carga aquí
        }
    }

    private void CheckDefeat()
    {
        if (currentRPM <= 0f)
        {
            SetDefeated();
        }
    }

    public void SetDefeated()
    {
        currentState = BeyBladeState.Defeated;
        physics.SetDefeated();
        //particleManager.PlayDefeatEffect();
        OnDefeated?.Invoke(this);
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    private void ResetToActiveState()
    {
        currentState = BeyBladeState.Active;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Arena_PowerUp"))
        {
            ArenaZone zone = other.GetComponent<ArenaZone>();
            //zone?.ApplyEffect(this);
        }
    }
}