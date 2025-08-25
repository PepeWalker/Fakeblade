using UnityEngine;

public class ArenaZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ZoneType zoneType = ZoneType.SpeedBoost;
    public float effectIntensity = 1f;
    public float effectDuration = 3f;
    public bool oneTimeUse = false;
    public float cooldownTime = 5f;

    [Header("Visual Effects")]
    public ParticleSystem zoneEffect;
    public Color zoneColor = Color.green;

    private bool isActive = true;
    private float cooldownTimer = 0f;

    public enum ZoneType
    {
        SpeedBoost,
        RPMRestore,
        SpecialPowerBoost,
        AttackBoost,
        SlowZone
    }

    private void Update()
    {
        if (!isActive && cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                ReactivateZone();
            }
        }
    }

    public void ApplyEffect(BeyBladeController blade)
    {
        if (!isActive) return;

        switch (zoneType)
        {
            case ZoneType.SpeedBoost:
                ApplySpeedBoost(blade);
                break;
            case ZoneType.RPMRestore:
                ApplyRPMRestore(blade);
                break;
            case ZoneType.SpecialPowerBoost:
                ApplySpecialPowerBoost(blade);
                break;
            case ZoneType.AttackBoost:
                ApplyAttackBoost(blade);
                break;
            case ZoneType.SlowZone:
                ApplySlowEffect(blade);
                break;
        }

        // Efectos visuales
        PlayZoneEffect(blade.transform.position);

        // Manejar uso único y cooldown
        if (oneTimeUse)
        {
            DeactivateZone();
        }
        else
        {
            StartCooldown();
        }
    }

    private void ApplySpeedBoost(BeyBladeController blade)
    {
        var rb = blade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 boost = rb.linearVelocity.normalized * effectIntensity * 5f;
            rb.AddForce(boost, ForceMode.VelocityChange);
        }

        Debug.Log($"{blade.name} received speed boost!");
    }

    private void ApplyRPMRestore(BeyBladeController blade)
    {
        float restoreAmount = blade.stats.maxRPM * (effectIntensity * 0.2f);
        blade.ModifyRPM(restoreAmount);

        Debug.Log($"{blade.name} restored {restoreAmount} RPM!");
    }

    private void ApplySpecialPowerBoost(BeyBladeController blade)
    {
        blade.AddSpecialPower(effectIntensity * 25f);

        Debug.Log($"{blade.name} gained special power!");
    }

    private void ApplyAttackBoost(BeyBladeController blade)
    {
        // Temporalmente aumentar el poder de ataque
        StartCoroutine(TemporaryAttackBoost(blade));
    }

    private void ApplySlowEffect(BeyBladeController blade)
    {
        var rb = blade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity *= (1f - effectIntensity * 0.5f);
        }

        Debug.Log($"{blade.name} was slowed down!");
    }

    private System.Collections.IEnumerator TemporaryAttackBoost(BeyBladeController blade)
    {
        // Este es un ejemplo de cómo podrías implementar un boost temporal
        // Necesitarías modificar BeyBladeStats para tener multiplicadores temporales

        yield return new WaitForSeconds(effectDuration);

        Debug.Log($"{blade.name} attack boost expired!");
    }

    private void PlayZoneEffect(Vector3 position)
    {
        if (zoneEffect != null)
        {
            zoneEffect.transform.position = position;
            zoneEffect.Play();
        }
    }

    private void StartCooldown()
    {
        isActive = false;
        cooldownTimer = cooldownTime;

        // Cambiar apariencia visual para mostrar que está en cooldown
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.gray;
        }
    }

    private void ReactivateZone()
    {
        isActive = true;

        // Restaurar apariencia visual
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = zoneColor;
        }
    }

    private void DeactivateZone()
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        // Configurar apariencia inicial
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = zoneColor;
        }
    }
}