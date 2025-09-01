using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BeyBladePhysics), typeof(Rigidbody))]
public class BeyBladeController : MonoBehaviour
{
    [Header("BeyBlade Configuration")]
    public BeyBladeStats stats;
    public int playerIndex = -1;
    public bool isAI = false;

    [Header("Current Status")]
    public float currentRPM;
    public float currentAttackCharges;
    public bool isDefeated = false;
    public bool isInvulnerable = false;

    [Header("Attack System")]
    public float chargedAttackTimer = 0f;
    public float maxChargeTime = 2f;
    public bool isChargingAttack = false;
    public float attackCooldown = 0f;

    [Header("Dash System")]
    public float dashCooldown = 0f;

    [Header("Special Power")]
    public float specialMeterCharge = 0f;
    public bool isSpecialActive = false;
    public float specialDuration = 0f;

    // References
    private BeyBladePhysics physics;
    private ParticleManager particleManager;
    private AudioSource audioSource;
    private Rigidbody rb;

    // Input handling
    private Vector2 currentMovementInput;
    private bool attackButtonDown = false;
    private bool attackButtonHeld = false;
    private bool dashButtonPressed = false;
    private bool specialButtonPressed = false;

    public delegate void BeyBladeEvent(BeyBladeController blade);
    public static event BeyBladeEvent OnBeyBladeDefeated;

    private void Awake()
    {
        physics = GetComponent<BeyBladePhysics>();
        particleManager = GetComponent<ParticleManager>();
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (isDefeated) return;

        UpdateCooldowns();
        UpdateSpecialPower();
        HandleChargedAttack();
    }

    private void FixedUpdate()
    {
        if (isDefeated) return;

        ProcessMovement();
        ProcessAttacks();
        ProcessDash();
        ProcessSpecial();
    }

    public void Initialize()
    {
        if (stats == null)
        {
            Debug.LogError($"BeyBladeStats not assigned for {gameObject.name}");
            return;
        }

        // Initialize stats
        currentRPM = stats.maxRPM;
        currentAttackCharges = stats.maxAttackCharges;
        specialMeterCharge = 0f;

        // Initialize physics
        physics.Initialize(stats);

        // Register with GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPlayer(this);
        }

        // Setup audio
        if (audioSource != null)
        {
            audioSource.volume = 0.3f;
            audioSource.loop = true;
            audioSource.pitch = 1f;
        }

        Debug.Log($"BeyBlade {gameObject.name} initialized with {stats.beyBladeName} stats");
    }

    #region Input Handling
    public void SetMovementInput(Vector2 input)
    {
        currentMovementInput = input;
    }

    public void SetAttackInput(bool buttonDown, bool buttonHeld)
    {
        attackButtonDown = buttonDown;
        attackButtonHeld = buttonHeld;
    }

    public void SetDashInput(bool pressed)
    {
        dashButtonPressed = pressed;
    }

    public void SetSpecialInput(bool pressed)
    {
        specialButtonPressed = pressed;
    }
    #endregion

    #region Movement & Actions
    private void ProcessMovement()
    {
        if (currentMovementInput.magnitude > 0.1f)
        {
            // Update forward direction for attacks
            Vector3 moveDirection = new Vector3(currentMovementInput.x, 0, currentMovementInput.y).normalized;
            transform.forward = moveDirection;

            physics.ApplyMovement(currentMovementInput);
        }
    }

    private void ProcessAttacks()
    {
        if (attackButtonDown && !isChargingAttack && currentAttackCharges > 0 && attackCooldown <= 0f)
        {
            StartAttack();
        }

        if (!attackButtonHeld && isChargingAttack)
        {
            ExecuteChargedAttack();
        }
    }

    private void ProcessDash()
    {
        if (dashButtonPressed && dashCooldown <= 0f && currentRPM >= stats.dashCost)
        {
            ExecuteDash();
        }
    }

    private void ProcessSpecial()
    {
        if (specialButtonPressed && specialMeterCharge >= 1f && !isSpecialActive)
        {
            ActivateSpecialPower();
        }
    }
    #endregion

    #region Combat System
    private void StartAttack()
    {
        isChargingAttack = true;
        chargedAttackTimer = 0f;

        // Set direction towards nearest enemy if no input
        if (currentMovementInput.magnitude < 0.1f)
        {
            BeyBladeController nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 directionToEnemy = (nearestEnemy.transform.position - transform.position).normalized;
                directionToEnemy.y = 0;
                transform.forward = directionToEnemy;
            }
        }

        Debug.Log($"{gameObject.name} started charging attack");
    }

    private void HandleChargedAttack()
    {
        if (isChargingAttack)
        {
            chargedAttackTimer += Time.deltaTime;

            // Auto-execute at max charge
            if (chargedAttackTimer >= maxChargeTime)
            {
                ExecuteChargedAttack();
            }
        }
    }

    private void ExecuteChargedAttack()
    {
        if (!isChargingAttack) return;

        float chargePercentage = Mathf.Clamp01(chargedAttackTimer / maxChargeTime);
        int chargesToUse = Mathf.RoundToInt(chargePercentage * currentAttackCharges);
        chargesToUse = Mathf.Clamp(chargesToUse, 1, (int)currentAttackCharges);

        // Execute attack
        if (chargePercentage < 0.3f)
        {
            physics.ExecuteAttack();
        }
        else
        {
            physics.ExecuteChargedAttack(chargePercentage);
        }

        // Consume charges
        currentAttackCharges -= chargesToUse;

        // Set cooldown
        attackCooldown = 0.5f;

        // Effects
        if (particleManager != null)
        {
            particleManager.PlayAttackEffect(chargePercentage);
        }

        // Reset charging
        isChargingAttack = false;
        chargedAttackTimer = 0f;

        // Build special meter
        BuildSpecialMeter(chargesToUse * 0.1f);

        Debug.Log($"Attack executed with {chargePercentage:F2} charge using {chargesToUse} charges");
    }

    private void ExecuteDash()
    {
        // Set direction if no input
        if (currentMovementInput.magnitude < 0.1f)
        {
            BeyBladeController nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 directionToEnemy = (nearestEnemy.transform.position - transform.position).normalized;
                directionToEnemy.y = 0;
                transform.forward = directionToEnemy;
            }
        }

        physics.ExecuteDash();
        ModifyRPM(-stats.dashCost);
        dashCooldown = 1f;

        if (particleManager != null)
        {
            particleManager.PlayDashEffect();
        }

        Debug.Log($"{gameObject.name} executed dash");
    }
    #endregion

    #region Special Powers
    private void ActivateSpecialPower()
    {
        if (specialMeterCharge < 1f) return;

        isSpecialActive = true;
        specialDuration = 5f; // Base duration
        specialMeterCharge = 0f;

        // Apply special effect based on type
        switch (stats.specialType)
        {
            case SpecialType.FireTrail:
                ActivateFireTrail();
                break;
            case SpecialType.ElectricDash:
                ActivateElectricDash();
                break;
            case SpecialType.StormBreaker:
                ActivateStormBreaker();
                break;
        }

        if (particleManager != null)
        {
            particleManager.PlaySpecialEffect(stats.specialType);
        }

        Debug.Log($"Special power activated: {stats.specialName}");
    }

    private void ActivateFireTrail()
    {
        physics.ModifySpeedLimits(1.3f, 1.5f, specialDuration);
        // TODO: Implement fire trail damage zone
    }

    private void ActivateElectricDash()
    {
        dashCooldown = 0f; // Reset dash cooldown
        physics.ModifySpeedLimits(1.2f, 2f, specialDuration);
    }

    private void ActivateStormBreaker()
    {
        // Massive defense boost and attack power
        rb.mass *= 2f;
        StartCoroutine(RestoreMassAfterSpecial());
    }

    private IEnumerator RestoreMassAfterSpecial()
    {
        yield return new WaitForSeconds(specialDuration);
        rb.mass = stats.mass;
    }
    #endregion

    #region Status Management
    public void ModifyRPM(float amount)
    {
        float oldRPM = currentRPM;
        currentRPM = Mathf.Clamp(currentRPM + amount, 0, stats.maxRPM);

        if (amount < 0)
        {
            // Build special meter when taking damage
            BuildSpecialMeter(-amount * 0.01f);
        }

        if (currentRPM <= 0 && !isDefeated)
        {
            SetDefeated();
        }

        // Update audio based on RPM
        if (audioSource != null && audioSource.clip != null)
        {
            float rpmPercentage = currentRPM / stats.maxRPM;
            audioSource.pitch = Mathf.Lerp(0.5f, 1.5f, rpmPercentage);
            audioSource.volume = Mathf.Lerp(0.1f, 0.4f, rpmPercentage);
        }
    }

    private void BuildSpecialMeter(float amount)
    {
        specialMeterCharge = Mathf.Clamp01(specialMeterCharge + amount);
    }

    public void SetDefeated()
    {
        if (isDefeated) return;

        isDefeated = true;
        physics.SetDefeated();

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        OnBeyBladeDefeated?.Invoke(this);
        Debug.Log($"{gameObject.name} has been defeated!");
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }
    #endregion

    #region Utility Methods
    private BeyBladeController FindNearestEnemy()
    {
        BeyBladeController nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var player in GameManager.Instance.activePlayers)
        {
            if (player != this && !player.isDefeated)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }
        }

        return nearest;
    }

    private void UpdateCooldowns()
    {
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        if (dashCooldown > 0f)
            dashCooldown -= Time.deltaTime;

        // Recover attack charges
        if (currentAttackCharges < stats.maxAttackCharges)
        {
            float recoveryTime = stats.chargeRecoveryTime;
            if (Time.time % recoveryTime < Time.deltaTime)
            {
                currentAttackCharges = Mathf.Min(currentAttackCharges + 1, stats.maxAttackCharges);
            }
        }
    }

    private void UpdateSpecialPower()
    {
        if (isSpecialActive)
        {
            specialDuration -= Time.deltaTime;
            if (specialDuration <= 0f)
            {
                isSpecialActive = false;
                Debug.Log($"Special power ended for {gameObject.name}");
            }
        }
    }

    public ParticleManager GetParticleManager()
    {
        return particleManager;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 2f);

            // Direction indicator
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);

            // Special meter indicator
            if (specialMeterCharge > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up, specialMeterCharge * 0.5f);
            }
        }
    }
    #endregion
}