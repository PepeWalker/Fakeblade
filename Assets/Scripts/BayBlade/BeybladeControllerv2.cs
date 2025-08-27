using UnityEngine;
using UnityEngine.InputSystem;

public class ImprovedBeyBladeController : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool useCameraRelativeMovement = true;

    [Header("Control Settings")]
    [SerializeField] private float movementDeadzone = 0.1f;
    [SerializeField] private float attackDeadzone = 0.3f;

    [Header("Components")]
    private BeyBladePhysics physics;
    private BeyBladeStats stats;
    private PlayerInput playerInput;

    [Header("Input Actions")]
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dashAction;
    private InputAction chargeAttackAction;

    [Header("Control State")]
    private Vector2 currentMovementInput;
    private Vector2 currentAttackDirection;
    private bool isChargingAttack = false;
    private float chargeStartTime = 0f;

    [Header("Team Settings")]
    [SerializeField] private int playerTeam = 0; // 0 = Equipo 1, 1 = Equipo 2, etc.
    [SerializeField] private int playerIndex = -1;

    // RPM y combate
    [Header("Combat Stats")]
    public float currentRPM;
    public int attackCharges;
    public float specialCharge = 0f;

    private void Awake()
    {
        physics = GetComponent<BeyBladePhysics>();
        playerInput = GetComponent<PlayerInput>();

        // Auto-encontrar cámara si no está asignada
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraTransform = mainCam.transform;
        }

        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (playerInput == null) return;

        // Obtener acciones del Input Action Asset
        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];
        dashAction = playerInput.actions["Dash"];
        chargeAttackAction = playerInput.actions["ChargeAttack"];

        // Suscribir eventos
        attackAction.performed += OnAttackPerformed;
        dashAction.performed += OnDashPerformed;
        chargeAttackAction.started += OnChargeAttackStarted;
        chargeAttackAction.canceled += OnChargeAttackCanceled;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleAttackCharging();
    }

    private void HandleMovementInput()
    {
        // Leer input de movimiento
        currentMovementInput = moveAction.ReadValue<Vector2>();

        // Aplicar deadzone
        if (currentMovementInput.magnitude < movementDeadzone)
        {
            currentMovementInput = Vector2.zero;
        }

        // Convertir a movimiento relativo a la cámara
        Vector2 movementInput = GetCameraRelativeMovement(currentMovementInput);

        // Aplicar movimiento a la física
        if (physics != null)
        {
            physics.ApplyMovement(movementInput);
        }
    }

    private Vector2 GetCameraRelativeMovement(Vector2 input)
    {
        if (!useCameraRelativeMovement || cameraTransform == null || input == Vector2.zero)
            return input;

        // Obtener la dirección forward y right de la cámara (solo en el plano horizontal)
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Proyectar en el plano horizontal (Y = 0)
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calcular dirección final
        Vector3 desiredDirection = (cameraRight * input.x) + (cameraForward * input.y);

        return new Vector2(desiredDirection.x, desiredDirection.z);
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (CanAttack())
        {
            ExecuteNormalAttack();
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (CanDash())
        {
            ExecuteDash();
        }
    }

    private void OnChargeAttackStarted(InputAction.CallbackContext context)
    {
        if (CanAttack())
        {
            StartChargeAttack();
        }
    }

    private void OnChargeAttackCanceled(InputAction.CallbackContext context)
    {
        if (isChargingAttack)
        {
            ExecuteChargedAttack();
        }
    }

    private void ExecuteNormalAttack()
    {
        // Determinar dirección del ataque
        Vector2 attackDirection = GetAttackDirection();

        // Aplicar dirección al transform antes del ataque
        if (attackDirection != Vector2.zero)
        {
            Vector3 attackDir3D = new Vector3(attackDirection.x, 0, attackDirection.y);
            transform.forward = attackDir3D.normalized;
        }

        // Ejecutar ataque
        physics?.ExecuteAttack();
        ConsumeAttackCharge();

        Debug.Log($"Normal Attack executed in direction: {attackDirection}");
    }

    private void ExecuteDash()
    {
        // Determinar dirección del dash
        Vector2 dashDirection = GetMovementOrAttackDirection();

        // Si no hay input, dash hacia adelante
        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(transform.forward.x, transform.forward.z);
        }

        // Aplicar dirección al transform
        Vector3 dashDir3D = new Vector3(dashDirection.x, 0, dashDirection.y);
        transform.forward = dashDir3D.normalized;

        // Ejecutar dash
        physics?.ExecuteDash();
        ModifyRPM(-stats.dashCost);

        Debug.Log($"Dash executed in direction: {dashDirection}");
    }

    private Vector2 GetAttackDirection()
    {
        // Prioridad: 1) Stick derecho si está siendo usado, 2) Stick izquierdo, 3) Dirección actual
        Vector2 rightStickInput = GetRightStickInput();

        if (rightStickInput.magnitude > attackDeadzone)
        {
            return GetCameraRelativeMovement(rightStickInput);
        }

        return GetMovementOrAttackDirection();
    }

    private Vector2 GetMovementOrAttackDirection()
    {
        // Si hay input de movimiento, usarlo
        if (currentMovementInput.magnitude > movementDeadzone)
        {
            return GetCameraRelativeMovement(currentMovementInput);
        }

        // Si no, usar dirección actual del beyblade
        return new Vector2(transform.forward.x, transform.forward.z);
    }

    private Vector2 GetRightStickInput()
    {
        // Intentar obtener input del stick derecho si existe
        try
        {
            if (playerInput.actions.FindAction("Look") != null)
            {
                return playerInput.actions["Look"].ReadValue<Vector2>();
            }
        }
        catch
        {
            // Si no existe la acción "Look", no hay problema
        }

        return Vector2.zero;
    }

    private void StartChargeAttack()
    {
        isChargingAttack = true;
        chargeStartTime = Time.time;

        Debug.Log("Started charging attack");
    }

    private void HandleAttackCharging()
    {
        if (isChargingAttack)
        {
            float chargeTime = Time.time - chargeStartTime;

            // Auto-ejecutar si se carga por demasiado tiempo
            if (chargeTime >= stats.chargeRecoveryTime * 2f)
            {
                ExecuteChargedAttack();
            }
        }
    }

    private void ExecuteChargedAttack()
    {
        if (!isChargingAttack) return;

        float chargeTime = Time.time - chargeStartTime;
        float chargePercentage = Mathf.Clamp01(chargeTime / stats.chargeRecoveryTime);

        // Determinar dirección del ataque cargado
        Vector2 attackDirection = GetAttackDirection();

        // Aplicar dirección
        if (attackDirection != Vector2.zero)
        {
            Vector3 attackDir3D = new Vector3(attackDirection.x, 0, attackDirection.y);
            transform.forward = attackDir3D.normalized;
        }

        // Ejecutar ataque cargado (más poderoso)
        physics?.ExecuteChargedAttack(chargePercentage);
        ConsumeAttackCharge();

        isChargingAttack = false;

        Debug.Log($"Charged Attack executed with {chargePercentage:F2} charge in direction: {attackDirection}");
    }

    #region Combat System

    public bool CanAttack()
    {
        return attackCharges > 0 && currentRPM > 0;
    }

    public bool CanDash()
    {
        return currentRPM >= stats.dashCost;
    }

    public void ConsumeAttackCharge()
    {
        attackCharges = Mathf.Max(0, attackCharges - 1);
    }

    public void ModifyRPM(float amount)
    {
        currentRPM = Mathf.Max(0, currentRPM + amount);

        if (currentRPM <= 0)
        {
            OnDefeat();
        }
    }

    private void OnDefeat()
    {
        physics?.SetDefeated();

        Debug.Log($"{gameObject.name} has been defeated!");

        // Notificar al GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDefeated(this);
        }
    }

    #endregion

    #region Camera Management

    public void SetCameraReference(Transform camera)
    {
        cameraTransform = camera;
    }

    public void SetCameraRelativeMovement(bool enabled)
    {
        useCameraRelativeMovement = enabled;
    }

    #endregion

    private void OnEnable()
    {
        SetupInputActions();
    }

    private void OnDisable()
    {
        // Desuscribir eventos
        if (attackAction != null) attackAction.performed -= OnAttackPerformed;
        if (dashAction != null) dashAction.performed -= OnDashPerformed;
        if (chargeAttackAction != null)
        {
            chargeAttackAction.started -= OnChargeAttackStarted;
            chargeAttackAction.canceled -= OnChargeAttackCanceled;
        }
    }
}