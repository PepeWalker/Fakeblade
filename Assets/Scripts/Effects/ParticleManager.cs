using UnityEngine;
using System.Collections;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem sparkEffect;          // Chispas al colisionar
    public ParticleSystem dashEffect;           // Efecto al hacer dash
    public ParticleSystem defeatEffect;        // Efecto de derrota
    public ParticleSystem trailEffect;         // Rastro continuo
    public ParticleSystem powerUpEffect;       // Efecto de power-up
    public ParticleSystem specialAttackEffect; // Ataque especial

    [Header("Trail System")]
    public TrailRenderer trailRenderer;
    public ParticleSystem continuousTrail;      // Rastro de partículas continuo

    [Header("Impact Effects")]
    public ParticleSystem collisionSparks;     // Chispas de colisión
    public ParticleSystem groundSparks;        // Chispas contra el suelo

    private Color playerColor = Color.white;
    private BeyBladeController controller;
    private Rigidbody rb;

    // Control de intensidad basado en velocidad
    [Header("Dynamic Settings")]
    public float minSpeedForTrail = 2f;
    public float maxTrailIntensity = 100f;
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public void Initialize(Color color, BeyBladeController bladeController)
    {
        playerColor = color;
        controller = bladeController;
        rb = controller.GetComponent<Rigidbody>();

        SetupParticleColors();
        SetupTrailSystem();
        StartCoroutine(UpdateTrailBasedOnSpeed());
    }

    private void SetupParticleColors()
    {
        // Configurar colores base
        SetParticleColor(sparkEffect, playerColor);
        SetParticleColor(dashEffect, playerColor);
        SetParticleColor(trailEffect, playerColor);
        SetParticleColor(continuousTrail, playerColor);
        SetParticleColor(powerUpEffect, playerColor);

        // Chispas con colores más brillantes
        if (collisionSparks != null)
        {
            Color sparkColor = Color.Lerp(playerColor, Color.white, 0.5f);
            SetParticleColor(collisionSparks, sparkColor);
        }

        if (groundSparks != null)
        {
            Color groundColor = Color.Lerp(playerColor, Color.yellow, 0.3f);
            SetParticleColor(groundSparks, groundColor);
        }

        // Trail Renderer
        if (trailRenderer != null)
        {
            trailRenderer.startColor = playerColor;
            trailRenderer.endColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);
        }
    }

    private void SetupTrailSystem()
    {
        if (continuousTrail != null)
        {
            var emission = continuousTrail.emission;
            emission.enabled = true;
            emission.rateOverTime = 0; // Lo controlaremos manualmente
        }
    }

    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;

        var main = ps.main;
        main.startColor = color;

        // También configurar gradiente si existe
        var colorOverLifetime = ps.colorOverLifetime;
        if (colorOverLifetime.enabled)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0.0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(color, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.5f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;
        }
    }

    // Actualizar rastro basado en velocidad
    private IEnumerator UpdateTrailBasedOnSpeed()
    {
        while (controller != null && !controller.IsDefeated)
        {
            if (rb != null)
            {
                float currentSpeed = rb.linearVelocity.magnitude;
                float speedRatio = Mathf.Clamp01(currentSpeed / 15f); // Máximo a 15 unidades/seg

                UpdateTrailIntensity(speedRatio);
                UpdateGroundSparks(currentSpeed);
            }

            yield return new WaitForSeconds(0.1f); // Actualizar 10 veces por segundo
        }
    }

    private void UpdateTrailIntensity(float speedRatio)
    {
        // Trail Renderer
        if (trailRenderer != null)
        {
            trailRenderer.enabled = speedRatio > 0.1f;
            if (trailRenderer.enabled)
            {
                float alpha = intensityCurve.Evaluate(speedRatio);
                Color startColor = new Color(playerColor.r, playerColor.g, playerColor.b, alpha);
                trailRenderer.startColor = startColor;
            }
        }

        // Continuous Trail Particles
        if (continuousTrail != null)
        {
            var emission = continuousTrail.emission;
            if (speedRatio > 0.1f)
            {
                emission.rateOverTime = maxTrailIntensity * intensityCurve.Evaluate(speedRatio);

                // Ajustar velocidad de partículas
                var velocityOverLifetime = continuousTrail.velocityOverLifetime;
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                velocityOverLifetime.radial = -speedRatio * 2f; // Hacia afuera
            }
            else
            {
                emission.rateOverTime = 0;
            }
        }
    }

    private void UpdateGroundSparks(float speed)
    {
        if (groundSparks != null && speed > minSpeedForTrail)
        {
            // Verificar si está cerca del suelo
            if (Physics.Raycast(transform.position, Vector3.down, 1f))
            {
                if (!groundSparks.isPlaying)
                {
                    groundSparks.Play();
                }

                var emission = groundSparks.emission;
                emission.rateOverTime = speed * 10f; // Más velocidad = más chispas
            }
            else if (groundSparks.isPlaying)
            {
                groundSparks.Stop();
            }
        }
    }

    // Métodos de efectos específicos
    public void PlaySparkEffect(Vector3 position, Vector3 direction, float intensity = 1f)
    {
        if (sparkEffect != null)
        {
            sparkEffect.transform.position = position;
            sparkEffect.transform.LookAt(position + direction);

            var main = sparkEffect.main;
            main.startSpeed = intensity * 5f;

            var emission = sparkEffect.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)(20 * intensity))
            });

            sparkEffect.Play();
        }
    }

    public void PlayCollisionSparks(Vector3 position, float impactForce)
    {
        if (collisionSparks != null)
        {
            collisionSparks.transform.position = position;

            var main = collisionSparks.main;
            main.startSpeed = Mathf.Clamp(impactForce * 2f, 3f, 15f);

            var emission = collisionSparks.emission;
            int sparkCount = Mathf.RoundToInt(Mathf.Clamp(impactForce * 5f, 10f, 50f));
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)sparkCount)
            });

            collisionSparks.Play();
        }
    }

    public void PlayDashEffect(Vector3 direction)
    {
        if (dashEffect != null)
        {
            dashEffect.transform.LookAt(transform.position - direction);
            dashEffect.Play();
        }
    }

    public void PlaySpecialAttackEffect(string effectName)
    {
        switch (effectName.ToLower())
        {
            case "fire trail":
                StartCoroutine(FireTrailEffect());
                break;
            case "electric dash":
                StartCoroutine(ElectricDashEffect());
                break;
            case "storm breaker":
                StartCoroutine(StormBreakerEffect());
                break;
        }
    }

    public void PlayDefeatEffect()
    {
        if (defeatEffect != null)
        {
            defeatEffect.Play();
        }

        // Detener todos los efectos activos
        StopAllContinuousEffects();
    }

    public void StopAllContinuousEffects()
    {
        if (trailRenderer != null) trailRenderer.enabled = false;
        if (continuousTrail != null) continuousTrail.Stop();
        if (groundSparks != null) groundSparks.Stop();
    }

    // Efectos especiales específicos
    private IEnumerator FireTrailEffect()
    {
        Color originalColor = playerColor;
        playerColor = Color.red;
        SetupParticleColors();

        // Incrementar intensidad del rastro
        if (continuousTrail != null)
        {
            var main = continuousTrail.main;
            main.startLifetime = 2f; // Rastro más duradero

            var emission = continuousTrail.emission;
            emission.rateOverTime = maxTrailIntensity * 1.5f;
        }

        yield return new WaitForSeconds(5f); // Duración del efecto

        playerColor = originalColor;
        SetupParticleColors();

        // Restaurar valores normales
        if (continuousTrail != null)
        {
            var main = continuousTrail.main;
            main.startLifetime = 1f;
        }
    }

    private IEnumerator ElectricDashEffect()
    {
        if (specialAttackEffect != null)
        {
            var main = specialAttackEffect.main;
            main.startColor = Color.cyan;

            specialAttackEffect.Play();

            // Efecto de electricidad durante 3 segundos
            yield return new WaitForSeconds(3f);

            specialAttackEffect.Stop();
        }
    }

    private IEnumerator StormBreakerEffect()
    {
        if (powerUpEffect != null)
        {
            var main = powerUpEffect.main;
            main.startColor = Color.blue;
            main.startSize = 2f;

            powerUpEffect.Play();

            yield return new WaitForSeconds(4f);

            powerUpEffect.Stop();
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}