using UnityEngine;
using UnityEngine.Rendering;

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

    [Header("Spin Settings")]
    public static float SPIN_MULTI_NUM = 2f;
    public float spinDecayRate = 0f; // Velocidad de pérdida de giro

    private BeyBladeStats stats;
    private float currentSpinSpeed; // RPM visual
    private float actualSpinVelocity; // Velocidad real de giro
    private Vector3 lastVelocity;
    private BeyBladeController controller;

    public void Initialize(BeyBladeStats bladeStats)
    {
        stats = bladeStats;
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<BeyBladeController>();

        // Configurar propiedades físicas
        rb.mass = stats.mass;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.3f; // REDUCIDO para mejor giro

        // Iniciar rotación
        StartSpin();
    }

    private void FixedUpdate()
    {
        ApplyGyroscopicEffect();
        ApplySmartStabilization(); // NUEVO MÉTODO
        UpdateSpinDecay();
        UpdateSpinVisualization();
        HandleArenaCollision();
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
        rb.AddForce(movement, ForceMode.Acceleration);
    }

    public void ExecuteAttack()
    {
        // Aumentar masa temporalmente
        float originalMass = rb.mass;
        rb.mass += stats.attackPower * 0.1f;

        // Aplicar fuerza hacia adelante
        Vector3 attackDirection = transform.forward;
        rb.AddForce(attackDirection * stats.attackPower, ForceMode.Impulse);

        // Restaurar masa
        StartCoroutine(RestoreMassCoroutine(originalMass, 0.2f));
    }

    public void ExecuteDash()
    {
        Vector3 dashDirection = rb.linearVelocity.normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        rb.AddForce(dashDirection * stats.dashPower, ForceMode.Impulse);
    }

    // NUEVO: Método de estabilización inteligente
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
            rb.linearDamping = 0.5f;
        }
        else
        {
            rb.linearDamping = 0.1f;
        }
    }

    public void SetDefeated()
    {
        actualSpinVelocity = 0;
        rb.angularVelocity *= 0.1f;
        rb.linearVelocity *= 0.1f;
    }

    // NUEVO: Método para añadir/quitar spin
    public void ModifySpinVelocity(float amount)
    {
        actualSpinVelocity = Mathf.Max(0, actualSpinVelocity + amount);
    }

    // NUEVO: Obtener velocidad de giro actual
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
        if (collision.gameObject.CompareTag("BeyBlade"))
        {
            BeyBladeController otherBlade = collision.gameObject.GetComponent<BeyBladeController>();
            if (otherBlade != null && CombatSystem.Instance != null)
            {
                CombatSystem.Instance.HandleCollision(GetComponent<BeyBladeController>(), otherBlade, collision);
            }
        }
    }

    // GIZMOS para debug
    private void OnDrawGizmosSelected()
    {
        // Mostrar dirección de giro
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        if (actualSpinVelocity > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, Vector3.up * (actualSpinVelocity / 100f));
        }
    }
}