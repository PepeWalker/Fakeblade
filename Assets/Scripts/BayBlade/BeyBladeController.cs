using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using System.Collections;
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
    public Color playerColor = Color.red;

    [Header("Combat")]
    public float currentRPM;
    public int attackCharges;
    public float specialPower;

    [Header("Cooldowns")]
    public float dashCooldownTime = 1.5f;
    public float attackCooldownTime = 0.5f;
    public float chargeRecoveryRate = 1f; // Cargas por segundo

    // Timers privados
    private float dashCooldownTimer = 0f;
    private float attackCooldownTimer = 0f;
    private float chargeRecoveryTimer = 0f;



    // Propiedades públicas para verificar estado
    public bool IsDefeated => currentState == BeyBladeState.Defeated;
    public bool CanAttack => attackCharges > 0 && currentState == BeyBladeState.Active && attackCooldownTimer <= 0;
    public bool CanDash => currentRPM >= stats.dashCost && currentState == BeyBladeState.Active && dashCooldownTimer <= 0;


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
    public System.Action<BeyBladeController> OnDash;
    public System.Action<BeyBladeController> OnSpecialActivated;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        // Asegurar que los componentes estén asignados
        if (stats == null) stats = GetComponent<BeyBladeStats>();
        if (physics == null) physics = GetComponent<BeyBladePhysics>();
        if (particleManager == null) particleManager = GetComponent<ParticleManager>();

        currentRPM = stats.maxRPM;
        attackCharges = stats.maxAttackCharges;
        specialPower = 0f;

        physics.Initialize(stats);

        // Inicializar partículas si existe el componente
        if (particleManager != null)
        {
            particleManager = GetComponent<ParticleManager>();
        }

        // Registrarse en el GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }
    }

    private void Update()
    {
        if (currentState == BeyBladeState.Defeated) return;

        HandleInput();
        UpdateTimers();
        UpdateRPM();
        UpdateCharges();
        CheckDefeat();
    }

    private void UpdateTimers()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;
    }

    private void HandleInput()
    {
        //Obtener input del inputmanager
        Vector2 movement = Vector2.zero;
        bool attackInput = false;
        bool dashInput = false;
        bool specialInput = false;

        if (InputManager.Instance != null) {
            movement = InputManager.Instance.GetMovementInput(playerIndex);
            attackInput = InputManager.Instance.GetAttackInput(playerIndex);
            dashInput = InputManager.Instance.GetDashInput(playerIndex);
            specialInput = InputManager.Instance.GetSpecialInput(playerIndex);
        }

        // Aplicar movimiento
        if (movement != Vector2.zero)
        {
            physics.ApplyMovement(movement);
        }

        // Procesar inputs de acción
        if (attackInput) TryAttack();
        if (dashInput) TryDash(movement);
        if (specialInput) TrySpecialAttack();
    }

    public void TryAttack()
    {
        if (!CanAttack) return;

        ExecuteAttack();
    }

    public void ExecuteAttack()
    {
        currentState = BeyBladeState.Attacking;
        attackCharges--;
        attackCooldownTimer = attackCooldownTime;

        // Ejecutar ataque en physics
        physics.ExecuteAttack();

        // Efectos de partículas
        if (particleManager != null)
        {
            //particleManager.PlaySparkEffect(transform.position, transform.forward, 1f);
        }

        // Eventos y audio
        OnAttack?.Invoke(this);

        // Volver al estado activo después del ataque
        StartCoroutine(ResetStateCoroutine(0.2f, BeyBladeState.Active));
    }

    public void TryDash(Vector2 direction)
    {
        if (!CanDash) return;

        ExecuteDash(direction);
    }

    public bool ExecuteDash(Vector2 direction)
    {
        if (!CanDash) return false;

        dashCooldownTimer = dashCooldownTime;
        ModifyRPM(-stats.dashCost);

        Vector3 dashDirection = new Vector3(direction.x, 0, direction.y).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        // Ejecutar dash en physics
        physics.ExecuteDash();

        // Efectos de partículas de dash
        if (particleManager != null)
        {
            //particleManager.PlayDashEffect(dashDirection);
        }

        // Eventos
        OnDash?.Invoke(this);

        return true;
    }

    public void TrySpecialAttack()
    {
        if (specialPower >= 100f && currentState == BeyBladeState.Active)
        {
            ExecuteSpecialAttack();
        }
    }

    public void ExecuteSpecialAttack()
    {
        if (stats != null)
        {
            stats.ExecuteSpecialPower(this);
            specialPower = 0f;

            // Efectos de partículas especiales
            if (particleManager != null)
            {
                //particleManager.PlaySpecialAttackEffect(stats.specialName);
            }

            OnSpecialActivated?.Invoke(this);
        }
    }


    public void ModifyRPM(float amount)
    {
        float oldRPM = currentRPM;
        currentRPM = Mathf.Clamp(currentRPM + amount, 0f, stats.maxRPM);

        if (oldRPM != currentRPM)
        {
            OnRPMChanged?.Invoke(this, currentRPM);
        }
    }

    public void AddSpecialPower(float amount)
    {
        specialPower = Mathf.Clamp(specialPower + amount, 0f, 100f);
    }

    private void UpdateRPM()
    {
        if (stats.rpmDecayRate > 0)
        {
            float naturalDecay = stats.rpmDecayRate * Time.deltaTime;
            ModifyRPM(-naturalDecay);
        }
    }

    private void UpdateCharges()
    {
        if (attackCharges < stats.maxAttackCharges)
        {
            chargeRecoveryTimer += Time.deltaTime;

            if (chargeRecoveryTimer >= stats.chargeRecoveryTime)
            {
                attackCharges++;
                chargeRecoveryTimer = 0f;
            }
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

        // Efectos de derrota
        if (particleManager != null)
        {
            //particleManager.PlayDefeatEffect();
        }

        OnDefeated?.Invoke(this);

        // Notificar al GameManager si existe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPlayer(this);
        }
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    private IEnumerator ResetStateCoroutine(float delay, BeyBladeState newState)
    {
        yield return new WaitForSeconds(delay);
        currentState = newState;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Arena_PowerUp"))
        {
            ArenaZone zone = other.GetComponent<ArenaZone>();
            if (zone != null)
            {
                zone.ApplyEffect(this);
            }
        }
    }

    

    public ParticleManager GetParticleManager()
    {
        if (particleManager == null)
            particleManager = GetComponent<ParticleManager>();
        return particleManager;
    }

    // Método para obtener información del jugador, para UI sobretodo
    public float GetRPMPercentage()
    {
        return stats.maxRPM > 0 ? currentRPM / stats.maxRPM : 0f;
    }

    public float GetSpecialPercentage()
    {
        return specialPower / 100f;
    }

    private void OnDestroy()
    {
        // Limpiar eventos para evitar memory leaks
        OnDefeated = null;
        OnRPMChanged = null;
        OnAttack = null;
        OnDash = null;
        OnSpecialActivated = null;
    }

}