using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("Particle Systems")]
    public ParticleSystem attackEffect;
    public ParticleSystem dashEffect;
    public ParticleSystem defeatEffect;
    public ParticleSystem trailEffect;

    [Header("Trail")]
    public TrailRenderer trailRenderer;

    private Color playerColor = Color.white;

    public void Initialize(Color color)
    {
        playerColor = color;
        SetupParticleColors();
    }

    private void SetupParticleColors()
    {
        if (trailRenderer != null)
        {
            trailRenderer.startColor = playerColor;
            trailRenderer.endColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);
        }

        SetParticleColor(attackEffect, playerColor);
        SetParticleColor(dashEffect, playerColor);
        SetParticleColor(trailEffect, playerColor);
    }

    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;

        var main = ps.main;
        main.startColor = color;
    }

    public void PlayAttackEffect()
    {
        if (attackEffect != null)
            attackEffect.Play();
    }

    public void PlayDashEffect()
    {
        if (dashEffect != null)
            dashEffect.Play();
    }

    public void PlayDefeatEffect()
    {
        if (defeatEffect != null)
            defeatEffect.Play();
    }

    public void SetTrailActive(bool active)
    {
        if (trailRenderer != null)
            trailRenderer.enabled = active;
    }
}