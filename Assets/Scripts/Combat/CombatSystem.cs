using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    [Header("Combat Settings")]
    public float impactForceMultiplier = 2f;
    public float rpmLossMultiplier = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void HandleCollision(BeyBladeController blade1, BeyBladeController blade2, Collision collision)
    {
        // Calcular fuerzas de impacto
        Vector3 impactPoint = collision.contacts[0].point;
        Vector3 blade1Velocity = blade1.GetComponent<Rigidbody>().linearVelocity;
        Vector3 blade2Velocity = blade2.GetComponent<Rigidbody>().linearVelocity;

        // Determinar qui�n tiene m�s velocidad
        float blade1Speed = blade1Velocity.magnitude;
        float blade2Speed = blade2Velocity.magnitude;

        BeyBladeController stronger = blade1Speed > blade2Speed ? blade1 : blade2;
        BeyBladeController weaker = blade1Speed > blade2Speed ? blade2 : blade1;

        // Calcular da�o
        float speedDifference = Mathf.Abs(blade1Speed - blade2Speed);
        float damage = speedDifference * rpmLossMultiplier;

        // Aplicar da�o al m�s d�bil
        weaker.ModifyRPM(-damage);

        // Efectos visuales y audio
        PlayCollisionEffects(impactPoint);
        AudioManager.Instance?.PlayCollisionSound(speedDifference);

        Debug.Log($"Collision: {stronger.name} vs {weaker.name} - Damage: {damage}");
    }

    private void PlayCollisionEffects(Vector3 position)
    {
        // Aqu� puedes agregar efectos de part�culas en el punto de impacto
    }
}