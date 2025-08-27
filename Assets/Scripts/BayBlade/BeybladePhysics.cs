using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static BeyBladeStats;

[RequireComponent(typeof(Rigidbody))]
public class BeyBladePhysics : MonoBehaviour
{
    [Header("Physics Components")]
    public Rigidbody rb;
    public Transform spinMesh; // Solo para visualización

    [Header("Physics Settings")]
    public float gyroscopicForce = 10f;
    public float stabilizationForce = 5f;
    public float smoothStabilizator = 0.1f;
    public float maxTiltAngle = 15f; // Máximo ángulo de inclinación
    public LayerMask arenaLayer = 1;

    [Header("Speed Limits")]
    public float maxNormalSpeed = 8f;      // Velocidad máxima con movimiento normal
    public float maxDashSpeed = 15f;       // Velocidad máxima temporal tras dash/ataque
    public float maxAttackSpeed = 12f;     // Velocidad máxima tras ataque
    public float speedDecayRate = 2f;      // Qué tan rápido vuelve a la velocidad normal
    public AnimationCurve speedLimitCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Spin Settings")]
    public static float SPIN_MULTI_NUM = 2f;
    public float spinDecayRate = 0f; // Velocidad de pérdida de giro

    private BeyBladeStats stats;
    private float currentSpinSpeed; // RPM visual
    private float actualSpinVelocity; // Velocidad real de giro
    private Vector3 lastVelocity;
    private BeyBladeController controller;

    [Header("Collision Detection")]
    public float collisionCooldown = 0.1f; // Evitar múltiples colisiones muy rápidas
    private float lastCollisionTime = 0f;

    // Control de velocidad
    private float currentMaxSpeed;
    private float tempSpeedBoostTimer = 0f;
    private bool hasTempSpeedBoost = false;

    public void Initialize(BeyBladeStats bladeStats)
    {
        stats = bladeStats;
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<BeyBladeController>();

        // Configurar propiedades físicas
        rb.mass = stats.mass;
        rb.linearDamping = 0.8f; // Aumentado para mejor control
        rb.angularDamping = 0.3f;

        //Configurar velocidades segun arquetipo de beyblade
        SetSpeedLimitsByType();

        // Inicializar velocidad máxima
        currentMaxSpeed = maxNormalSpeed;

        // Iniciar rotación
        StartSpin();
    }

    private void FixedUpdate()
    {
        ApplyGyroscopicEffect();
        ApplySmartStabilization();
        UpdateSpinDecay();
        UpdateSpinVisualization();
        HandleArenaCollision();
        LimitSpeed();
        UpdateSpeedLimits();
    }


    //Método para configurar velocidades según el tipo
    private void SetSpeedLimitsByType()
    {
        if (stats == null) return;

        switch (stats.type)
        {
            case BeyBladeType.Agility:
                maxNormalSpeed = 10f;  // Más rápido
                maxDashSpeed = 18f;
                maxAttackSpeed = 14f;
                speedDecayRate = 3f;   // Decae más rápido la velocidad extra
                Debug.Log($"{gameObject.name} configured as AGILITY type - Max Speed: {maxNormalSpeed}");
                break;

            case BeyBladeType.Attack:
                maxNormalSpeed = 8f;   // Velocidad media
                maxDashSpeed = 15f;
                maxAttackSpeed = 13f;
                speedDecayRate = 2f;
                Debug.Log($"{gameObject.name} configured as ATTACK type - Max Speed: {maxNormalSpeed}");
                break;

            case BeyBladeType.Defense:
                maxNormalSpeed = 6f;   // Más lento pero resistente
                maxDashSpeed = 12f;
                maxAttackSpeed = 10f;
                speedDecayRate = 1.5f; // Mantiene velocidad extra más tiempo
                Debug.Log($"{gameObject.name} configured as DEFENSE type - Max Speed: {maxNormalSpeed}");
                break;

            case BeyBladeType.Balanced:
                maxNormalSpeed = 7f;   // Equilibrado
                maxDashSpeed = 14f;
                maxAttackSpeed = 11f;
                speedDecayRate = 2f;
                Debug.Log($"{gameObject.name} configured as BALANCED type - Max Speed: {maxNormalSpeed}");
                break;

            default:
                // Valores por defecto si no se reconoce el tipo
                maxNormalSpeed = 8f;
                maxDashSpeed = 15f;
                maxAttackSpeed = 12f;
                speedDecayRate = 2f;
                Debug.LogWarning($"{gameObject.name} - Unknown BeyBlade type, using default speeds");
                break;
        }
    }
    // Método público para reconfigurar velocidades (útil para power-ups)
    public void ModifySpeedLimits(float normalSpeedMultiplier, float dashSpeedMultiplier, float duration = 0f)
    {
        maxNormalSpeed *= normalSpeedMultiplier;
        maxDashSpeed *= dashSpeedMultiplier;
        maxAttackSpeed *= normalSpeedMultiplier;

        // Si tiene duración, restaurar después
        if (duration > 0f)
        {
            StartCoroutine(RestoreSpeedLimitsCoroutine(normalSpeedMultiplier, dashSpeedMultiplier, duration));
        }
    }

    private System.Collections.IEnumerator RestoreSpeedLimitsCoroutine(float normalMult, float dashMult, float duration)
    {
        yield return new WaitForSeconds(duration);

        // Restaurar velocidades originales
        maxNormalSpeed /= normalMult;
        maxDashSpeed /= dashMult;
        maxAttackSpeed /= normalMult;

        Debug.Log($"{gameObject.name} speed limits restored to normal");
    }

    private void LimitSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        if (currentSpeed > currentMaxSpeed)
        {
            // Reducir gradualmente la velocidad en lugar de cortarla bruscamente
            Vector3 limitedVelocity = horizontalVelocity.normalized * currentMaxSpeed;

            // Mantener la velocidad Y (salto/gravedad)
            limitedVelocity.y = rb.linearVelocity.y;

            // Aplicar suavemente usando Lerp para evitar cambios bruscos
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, limitedVelocity, Time.fixedDeltaTime * speedDecayRate);
        }
    }

    private void UpdateSpeedLimits()
    {
        if (hasTempSpeedBoost)
        {
            tempSpeedBoostTimer -= Time.fixedDeltaTime;

            if (tempSpeedBoostTimer <= 0f)
            {
                // Volver gradualmente a la velocidad normal
                hasTempSpeedBoost = false;
                currentMaxSpeed = maxNormalSpeed;
            }
            else
            {
                // Decaer gradualmente desde velocidad alta a normal
                float decayProgress = 1f - (tempSpeedBoostTimer / 3f); // 3 segundos de duración
                currentMaxSpeed = Mathf.Lerp(maxDashSpeed, maxNormalSpeed,
                    speedLimitCurve.Evaluate(decayProgress));
            }
        }
    }


    public void StartSpin()
    {
        actualSpinVelocity = stats.spinForce;
        currentSpinSpeed = actualSpinVelocity;

        // NO aplicar angularVelocity al rigidbody, solo visual
        // rb.angularVelocity = Vector3.up * currentSpinSpeed; // COMENTADO
    }

    public void ApplyMovement(Vector2 input)
    {
        Vector3 movement = new Vector3(input.x, 0, input.y) * stats.movementSpeed;

        // Aplicar fuerza pero respetando el límite de velocidad
        Vector3 targetVelocity = rb.linearVelocity + (movement * Time.fixedDeltaTime);
        Vector3 horizontalTarget = new Vector3(targetVelocity.x, 0, targetVelocity.z);

        if (horizontalTarget.magnitude <= currentMaxSpeed)
        {
            rb.AddForce(movement, ForceMode.Acceleration);
        }
        else
        {
            // Si estamos cerca del límite, aplicar menos fuerza
            float speedRatio = horizontalTarget.magnitude / currentMaxSpeed;
            float forceFactor = Mathf.Clamp01(2f - speedRatio); // Reducir fuerza gradualmente
            rb.AddForce(movement * forceFactor, ForceMode.Acceleration);
        }
    }

    public void ExecuteAttack()
    {
        // Aumentar masa temporalmente
        float originalMass = rb.mass;
        rb.mass += stats.attackPower * 0.1f;

        // Aplicar fuerza hacia adelante (el controller ya ha ajustado transform.forward)
        Vector3 attackDirection = transform.forward;
        rb.AddForce(attackDirection * stats.attackPower, ForceMode.Impulse);

        // Permitir velocidad temporal más alta tras ataque
        SetTemporarySpeedBoost(maxAttackSpeed, 2f);

        // Restaurar masa
        StartCoroutine(RestoreMassCoroutine(originalMass, 0.2f));

        Debug.Log($"Attack executed in direction: {attackDirection}");
    }

    public void ExecuteDash()
    {
        // Usar la dirección ya establecida por el controller
        Vector3 dashDirection = transform.forward;

        rb.AddForce(dashDirection * stats.dashPower, ForceMode.Impulse);

        // Permitir velocidad temporal más alta tras dash
        SetTemporarySpeedBoost(maxDashSpeed, 3f);

        Debug.Log($"Dash executed in direction: {dashDirection}");
    }


    public void ExecuteChargedAttack(float chargePercentage)
    {
        // Aumentar masa temporalmente (más que un ataque normal)
        float originalMass = rb.mass;
        float massBonus = stats.attackPower * 0.2f * (1f + chargePercentage);
        rb.mass += massBonus;

        // Calcular potencia del ataque cargado
        float attackPower = stats.attackPower * (1f + chargePercentage);

        // Aplicar fuerza hacia adelante
        Vector3 attackDirection = transform.forward;
        rb.AddForce(attackDirection * attackPower, ForceMode.Impulse);

        // Permitir velocidad temporal más alta tras ataque cargado
        float maxSpeedBonus = maxAttackSpeed * (1f + chargePercentage * 0.5f);
        SetTemporarySpeedBoost(maxSpeedBonus, 3f);

        // Restaurar masa
        StartCoroutine(RestoreMassCoroutine(originalMass, 0.3f));

        Debug.Log($"Charged attack executed with {chargePercentage:F2} charge and {attackPower:F1} power");
    }


    //Método para establecer boosts temporales de velocidad
    public void SetTemporarySpeedBoost(float newMaxSpeed, float duration)
    {
        currentMaxSpeed = newMaxSpeed;
        tempSpeedBoostTimer = duration;
        hasTempSpeedBoost = true;
    }

    //Método público para obtener información de velocidad
    public float GetCurrentSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        return horizontalVelocity.magnitude;
    }

    public float GetSpeedPercentage()
    {
        return GetCurrentSpeed() / maxNormalSpeed;
    }

    public float GetCurrentMaxSpeed()
    {
        return currentMaxSpeed;
    }



    //Método de estabilización inteligente
    private void ApplySmartStabilization()
    {
        // Solo estabilizar si está muy inclinado
        Vector3 upDirection = transform.up;
        float tiltAngle = Vector3.Angle(upDirection, Vector3.up);

        if (tiltAngle > maxTiltAngle) 
        {
            
            Vector3 targetUp = Vector3.up;
            Vector3 torqueAxis = Vector3.Cross(upDirection, targetUp);

            // Solo aplica torque en X y Z, NO en Y
            torqueAxis.y = 0;

            float torqueMagnitude = tiltAngle * stabilizationForce * smoothStabilizator; //smoothStabilizator para suavizar el efecto de estabilizarse
            rb.AddTorque(torqueAxis.normalized * torqueMagnitude, ForceMode.Acceleration);
        }
    }

    private void ApplyGyroscopicEffect()
    {
       


        // Efecto giroscópico basado en spin actual
        //Funciona mal
        
        if (actualSpinVelocity > 0)
        {
            Vector3 spinAxis = Vector3.up;
            Vector3 angularMomentum = spinAxis * actualSpinVelocity;

            // Aplicar efecto giroscópico para resistir cambios de orientación
            Vector3 gyroscopicTorque = Vector3.Cross(rb.angularVelocity, angularMomentum) * gyroscopicForce * 0.01f;

            // Solo en X y Z, preservar Y
            gyroscopicTorque.y = 0;
            rb.AddTorque(gyroscopicTorque, ForceMode.Acceleration);
        }
        
    }

    // Sistema de pérdida de giro separado, por defecto estará a 0, en modos supervivencia se puede poner un valor
    private void UpdateSpinDecay()
    {
        if (actualSpinVelocity > 0)
        {
            // Pérdida natural de giro
            float decay = spinDecayRate * Time.fixedDeltaTime;
            actualSpinVelocity = Mathf.Max(0, actualSpinVelocity - decay);

            // Actualizar RPM del controlador
            if (controller != null)
            {
                float rpmPercentage = actualSpinVelocity / stats.spinForce;
                controller.currentRPM = controller.stats.maxRPM * rpmPercentage;
            }
        }
    }

    private void UpdateSpinVisualization()
    {
        if (spinMesh != null && actualSpinVelocity > 0)
        {
            // Suavizar la velocidad visual
            currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, actualSpinVelocity, Time.fixedDeltaTime * 2f);

            // Rotar SOLO el mesh visual, no el rigidbody
            float rotationSpeed = currentSpinSpeed * Time.fixedDeltaTime * Mathf.Rad2Deg * SPIN_MULTI_NUM;
            spinMesh.Rotate(0, rotationSpeed, 0, Space.Self);
        }
    }

    private void HandleArenaCollision()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 0.1f, arenaLayer))
        {
            rb.linearDamping = 0.8f;
        }
        else
        {
            rb.linearDamping = 0.3f;
        }
    }

    public void SetDefeated()
    {
        actualSpinVelocity = 0;
        rb.angularVelocity *= 0.1f;
        rb.linearVelocity *= 0.1f;
        currentMaxSpeed = 0f;
    }

    // Método para añadir/quitar spin
    public void ModifySpinVelocity(float amount)
    {
        actualSpinVelocity = Mathf.Max(0, actualSpinVelocity + amount);
    }

    // Obtener velocidad de giro actual
    public float GetCurrentSpinVelocity()
    {
        return actualSpinVelocity;
    }

    // Corrutina para restaurar masa
    private System.Collections.IEnumerator RestoreMassCoroutine(float originalMass, float delay)
    {
        yield return new WaitForSeconds(delay);
        rb.mass = originalMass;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar cooldown
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        if (collision.gameObject.CompareTag("BeyBlade"))
        {
            BeyBladeController otherBlade = collision.gameObject.GetComponent<BeyBladeController>();
            if (otherBlade != null && otherBlade != controller) // Evitar auto-colisión
            {
                // Verificar que CombatSystem existe
                if (CombatSystem.Instance != null)
                {
                    // Llamar al sistema de combate que maneja todo
                    CombatSystem.Instance.HandleCollision(controller, otherBlade, collision);
                    lastCollisionTime = Time.time;

                    Debug.Log($"BeyBlade collision detected: {gameObject.name} vs {otherBlade.name}");
                }
                else
                {
                    Debug.LogError("CombatSystem.Instance is null!");
                }
            }
        }
        else if (collision.gameObject.CompareTag("Arena"))
        {
            HandleArenaImpact(collision);
        }
    }

    private float CalculateCollisionIntensity(Collision collision)
    {
        // Calcular intensidad basada en velocidad relativa
        Vector3 relativeVelocity = rb.linearVelocity;
        if (collision.rigidbody != null)
            relativeVelocity -= collision.rigidbody.linearVelocity;

        float intensity = relativeVelocity.magnitude;

        // También considerar la masa para el cálculo
        float combinedMass = rb.mass;
        if (collision.rigidbody != null)
            combinedMass += collision.rigidbody.mass;

        intensity *= (combinedMass / 4f); // Normalizar para masas típicas de 2-4

        return intensity;
    }

    private void HandleArenaImpact(Collision collision)
    {
        // Partículas menores para impactos contra la arena
        float impactForce = rb.linearVelocity.magnitude;
        if (impactForce > 3f && controller?.GetParticleManager() != null)
        {
            Vector3 contactPoint = collision.contacts[0].point;
            controller.GetParticleManager().PlayCollisionSparks(contactPoint, impactForce * 0.3f);
        }
    }

    // GIZMOS para debug
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && rb != null)
        {
            // Mostrar velocidad actual
            Gizmos.color = Color.blue;
            Vector3 velocityDirection = rb.linearVelocity.normalized;
            Gizmos.DrawRay(transform.position, velocityDirection * GetCurrentSpeed());

            // Mostrar límite de velocidad
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, currentMaxSpeed * 0.1f);

            // Mostrar dirección de giro
            Gizmos.color = Color.green;
            if (actualSpinVelocity > 0)
            {
                Gizmos.DrawRay(transform.position, Vector3.up * (actualSpinVelocity / 100f));
            }
        }
    }
}