using UnityEngine;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem trailEffect;
    public ParticleSystem sparkEffect;
    public ParticleSystem collisionEffect;
    public ParticleSystem attackEffect;
    public ParticleSystem dashEffect;
    public ParticleSystem specialEffect;

    [Header("Trail Settings")]
    public TrailRenderer speedTrail;
    public float minSpeedForTrail = 3f;

    private BeyBladeController controller;
    private BeyBladePhysics physics;

    private void Awake()
    {
        controller = GetComponent<BeyBladeController>();
        physics = GetComponent<BeyBladePhysics>();
    }

    private void Start()
    {
        InitializeParticles();
    }

    private void Update()
    {
        UpdateSpeedTrail();
        UpdateSpinEffects();
    }

    private void InitializeParticles()
    {
        // Apply blade-specific colors and settings
        if (controller?.stats?.particleEffects != null)
        {
            controller.stats.particleEffects.ApplyToParticleManager(this);
        }

        // Initialize trail
        if (speedTrail != null)
        {
            speedTrail.enabled = false;
            if (controller?.stats?.trailMaterial != null)
            {
                speedTrail.material = controller.stats.trailMaterial;
            }
        }
    }

    private void UpdateSpeedTrail()
    {
        if (speedTrail != null && physics != null)
        {
            float currentSpeed = physics.GetCurrentSpeed();
            bool shouldShowTrail = currentSpeed > minSpeedForTrail;

            if (speedTrail.enabled != shouldShowTrail)
            {
                speedTrail.enabled = shouldShowTrail;
            }

            if (shouldShowTrail)
            {
                // Adjust trail width based on speed
                float speedPercentage = currentSpeed / 10f; // Max speed for calculation
                speedTrail.startWidth = Mathf.Lerp(0.1f, 0.3f, speedPercentage);
                speedTrail.endWidth = 0.05f;
            }
        }
    }

    private void UpdateSpinEffects()
    {
        if (trailEffect != null && controller != null)
        {
            float rpmPercentage = controller.currentRPM / controller.stats.maxRPM;

            // Adjust trail intensity based on RPM
            var emission = trailEffect.emission;
            emission.rateOverTime = rpmPercentage * 20f;

            var main = trailEffect.main;
            main.startSpeed = rpmPercentage * 5f;
        }
    }

    public void PlayCollisionSparks(Vector3 position, float intensity)
    {
        if (collisionEffect != null)
        {
            collisionEffect.transform.position = position;

            var main = collisionEffect.main;
            main.startSpeed = intensity * 3f;

            var emission = collisionEffect.emission;
            int burstCount = Mathf.RoundToInt(intensity * 15f);
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(burstCount, 5, 50))
            });

            collisionEffect.Play();
        }

        if (sparkEffect != null)
        {
            sparkEffect.transform.position = position;
            sparkEffect.Play();
        }
    }

    public void PlayAttackEffect(float chargePercentage)
    {
        if (attackEffect != null)
        {
            var main = attackEffect.main;
            main.startSize = Mathf.Lerp(0.2f, 0.8f, chargePercentage);
            main.startSpeed = Mathf.Lerp(2f, 8f, chargePercentage);

            var emission = attackEffect.emission;
            int burstCount = Mathf.RoundToInt(chargePercentage * 30f + 10f);
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)burstCount)
            });

            attackEffect.Play();
        }
    }

    public void PlayDashEffect()
    {
        if (dashEffect != null)
        {
            dashEffect.Play();
        }

        // Intensify trail temporarily
        if (speedTrail != null)
        {
            StartCoroutine(IntensifyTrail(1f));
        }
    }

    public void PlaySpecialEffect(SpecialType specialType)
    {
        if (specialEffect != null)
        {
            // Customize effect based on special type
            var main = specialEffect.main;

            switch (specialType)
            {
                case SpecialType.FireTrail:
                    main.startColor = Color.red;
                    break;
                case SpecialType.ElectricDash:
                    main.startColor = Color.cyan;
                    break;
                case SpecialType.StormBreaker:
                    main.startColor = Color.white;
                    break;
            }

            specialEffect.Play();
        }
    }

    private IEnumerator IntensifyTrail(float duration)
    {
        if (speedTrail != null)
        {
            float originalWidth = speedTrail.startWidth;
            speedTrail.startWidth *= 2f;

            yield return new WaitForSeconds(duration);

            speedTrail.startWidth = originalWidth;
        }
    }

    public void StopAllEffects()
    {
        ParticleSystem[] allParticles = { trailEffect, sparkEffect, collisionEffect, attackEffect, dashEffect, specialEffect };

        foreach (var ps in allParticles)
        {
            if (ps != null)
                ps.Stop();
        }

        if (speedTrail != null)
            speedTrail.enabled = false;
    }
}