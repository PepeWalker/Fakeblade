using UnityEngine;
using System.Collections.Generic;

public class ArenaZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ZoneType zoneType = ZoneType.SpeedBoost;
    public float zoneRadius = 2f;
    public bool isActive = true;
    public float effectStrength = 1f;
    public float effectDuration = 3f;

    [Header("Visual Effects")]
    public ParticleSystem zoneEffect;
    public Material zoneMaterial;
    public Color zoneColor = Color.green;

    [Header("Audio")]
    public AudioClip enterZoneSound;
    public AudioClip exitZoneSound;

    private List<BeyBladeController> bladesInZone = new List<BeyBladeController>();
    private SphereCollider zoneCollider;
    private MeshRenderer zoneRenderer;

    public void Initialize()
    {
        SetupCollider();
        SetupVisuals();
        ApplyZoneEffects();
    }

    private void SetupCollider()
    {
        zoneCollider = GetComponent<SphereCollider>();
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<SphereCollider>();
        }

        zoneCollider.isTrigger = true;
        zoneCollider.radius = zoneRadius;
    }

    private void SetupVisuals()
    {
        zoneRenderer = GetComponent<MeshRenderer>();
        if (zoneRenderer != null && zoneMaterial != null)
        {
            zoneRenderer.material = zoneMaterial;
            zoneRenderer.material.color = zoneColor;
        }

        if (zoneEffect != null)
        {
            var main = zoneEffect.main;
            main.startColor = zoneColor;
            zoneEffect.Play();
        }
    }

    private void ApplyZoneEffects()
    {
        // Configure zone based on type
        switch (zoneType)
        {
            case ZoneType.SpeedBoost:
                zoneColor = Color.green;
                break;
            case ZoneType.AttackBoost:
                zoneColor = Color.red;
                break;
            case ZoneType.DefenseBoost:
                zoneColor = Color.blue;
                break;
            case ZoneType.RPMRestore:
                zoneColor = Color.yellow;
                break;
            case ZoneType.Hazard:
                zoneColor = Color.magenta;
                break;
        }
    }

    public void UpdateZone()
    {
        // Apply continuous effects to blades in zone
        foreach (var blade in bladesInZone)
        {
            if (blade != null && !blade.isDefeated)
            {
                ApplyZoneEffect(blade);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        BeyBladeController blade = other.GetComponent<BeyBladeController>();
        if (blade != null && isActive)
        {
            OnBladeEnterZone(blade);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BeyBladeController blade = other.GetComponent<BeyBladeController>();
        if (blade != null)
        {
            OnBladeExitZone(blade);
        }
    }

    private void OnBladeEnterZone(BeyBladeController blade)
    {
        if (!bladesInZone.Contains(blade))
        {
            bladesInZone.Add(blade);

            // Play enter effects
            if (AudioManager.Instance != null && enterZoneSound != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(enterZoneSound, transform.position);
            }

            // Apply immediate effects
            ApplyZoneEffect(blade);

            Debug.Log($"{blade.name} entered {zoneType} zone");
        }
    }

    private void OnBladeExitZone(BeyBladeController blade)
    {
        if (bladesInZone.Contains(blade))
        {
            bladesInZone.Remove(blade);

            // Play exit effects
            if (AudioManager.Instance != null && exitZoneSound != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(exitZoneSound, transform.position);
            }

            Debug.Log($"{blade.name} exited {zoneType} zone");
        }
    }

    private void ApplyZoneEffect(BeyBladeController blade)
    {
        switch (zoneType)
        {
            case ZoneType.SpeedBoost:
                ApplySpeedBoost(blade);
                break;
            case ZoneType.AttackBoost:
                ApplyAttackBoost(blade);
                break;
            case ZoneType.DefenseBoost:
                ApplyDefenseBoost(blade);
                break;
            case ZoneType.RPMRestore:
                ApplyRPMRestore(blade);
                break;
            case ZoneType.Hazard:
                ApplyHazardDamage(blade);
                break;
        }
    }

    private void ApplySpeedBoost(BeyBladeController blade)
    {
        BeyBladePhysics physics = blade.GetComponent<BeyBladePhysics>();
        if (physics != null)
        {
            physics.ModifySpeedLimits(1f + effectStrength, 1f + effectStrength, effectDuration);
        }
    }

    private void ApplyAttackBoost(BeyBladeController blade)
    {
        // Restore attack charges faster
        // This would need to be implemented in BeyBladeController
        blade.currentAttackCharges = Mathf.Min(blade.currentAttackCharges + effectStrength * Time.deltaTime,
                                              blade.stats.maxAttackCharges);
    }

    private void ApplyDefenseBoost(BeyBladeController blade)
    {
        // Temporary invulnerability or reduced damage
        blade.isInvulnerable = true;
        StartCoroutine(RemoveInvulnerability(blade, effectDuration));
    }
    private void ApplyRPMRestore(BeyBladeController blade)
    {
        float restoreAmount = effectStrength * 10f * Time.deltaTime;
        blade.ModifyRPM(restoreAmount);
    }

    private void ApplyHazardDamage(BeyBladeController blade)
    {
        float damageAmount = effectStrength * 20f * Time.deltaTime;
        blade.ModifyRPM(-damageAmount);
    }

    private System.Collections.IEnumerator RemoveInvulnerability(BeyBladeController blade, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (blade != null)
        {
            blade.isInvulnerable = false;
        }
    }

    public Color GetZoneColor()
    {
        return zoneColor;
    }

    public float GetZoneRadius()
    {
        return zoneRadius;
    }
}

public enum ZoneType
{
    SpeedBoost,
    AttackBoost,
    DefenseBoost,
    RPMRestore,
    Hazard
}