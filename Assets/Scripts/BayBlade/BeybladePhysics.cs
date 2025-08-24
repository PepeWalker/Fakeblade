using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BeyBladePhysics : MonoBehaviour
{
    [Header("Physics Components")]
    public Rigidbody rb;
    public Transform spinMesh;

    [Header("Physics Settings")]
    public float gyroscopicForce = 10f;
    public float stabilizationForce = 5f;
    public LayerMask arenaLayer = 1;

    private BeyBladeStats stats;
    private float currentSpinSpeed;
    private Vector3 lastVelocity;

    public void Initialize(BeyBladeStats bladStats)
    {
        stats = bladStats;
        rb = GetComponent<Rigidbody>();

        // Configurar propiedades f�sicas
        rb.mass = stats.mass;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1f;

        // Iniciar rotaci�n
        StartSpin();
    }

    private void FixedUpdate()
    {
        ApplyGyroscopicEffect();
        ApplyStabilization();
        UpdateSpinVisualization();
        HandleArenaCollision();
    }

    public void StartSpin()
    {
        currentSpinSpeed = stats.spinForce;
        rb.angularVelocity = Vector3.up * currentSpinSpeed;
    }

    public void ApplyMovement(Vector2 input)
    {
        Vector3 movement = new Vector3(input.x, 0, input.y) * stats.movementSpeed;
        rb.AddForce(movement, ForceMode.Acceleration);
    }

    public void ExecuteAttack()
    {
        // Aumentar masa temporalmente para el ataque
        rb.mass += stats.attackPower * 0.1f;

        // Aplicar fuerza de ataque
        Vector3 attackDirection = transform.forward;
        rb.AddForce(attackDirection * stats.attackPower, ForceMode.Impulse);

        // Restaurar masa despu�s del ataque
        Invoke(nameof(RestoreMass), 0.2f);
    }

    public void ExecuteDash()
    {
        Vector3 dashDirection = rb.linearVelocity.normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        rb.AddForce(dashDirection * stats.dashPower, ForceMode.Impulse);
    }

    private void ApplyGyroscopicEffect()
    {
        // Simular efecto girosc�pico para estabilidad
        Vector3 gyroscopicTorque = Vector3.Cross(transform.up, rb.angularVelocity) * gyroscopicForce;
        rb.AddTorque(gyroscopicTorque, ForceMode.Acceleration);
    }

    private void ApplyStabilization()
    {
        // Mantener el trompo erguido
        Vector3 stabilizationTorque = Vector3.Cross(transform.up, Vector3.up) * stabilizationForce;
        rb.AddTorque(stabilizationTorque, ForceMode.Acceleration);
    }

    private void UpdateSpinVisualization()
    {
        if (spinMesh != null)
        {
            currentSpinSpeed = Mathf.Lerp(currentSpinSpeed, rb.angularVelocity.magnitude, Time.fixedDeltaTime);
            spinMesh.Rotate(0, currentSpinSpeed * Time.fixedDeltaTime * Mathf.Rad2Deg, 0, Space.Self);
        }
    }

    private void HandleArenaCollision()
    {
        // Detectar si est� tocando la arena
        if (Physics.Raycast(transform.position, Vector3.down, 0.1f, arenaLayer))
        {
            // Aplicar fricci�n de la arena
            rb.linearDamping = 0.5f;
        }
        else
        {
            // En el aire, menos resistencia
            rb.linearDamping = 0.1f;
        }
    }

    public void SetDefeated()
    {
        // Detener el trompo gradualmente
        rb.angularVelocity *= 0.1f;
        rb.linearVelocity *= 0.1f;
    }

    private void RestoreMass()
    {
        rb.mass = stats.mass;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("BeyBlade"))
        {
            BeyBladeController otherBlade = collision.gameObject.GetComponent<BeyBladeController>();
            if (otherBlade != null)
            {
                CombatSystem.Instance.HandleCollision(GetComponent<BeyBladeController>(), otherBlade, collision);
            }
        }
    }
}