using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("Player Info")]
    public Text playerNameText;
    public Image playerColorImage;

    [Header("RPM Bar")]
    public Slider rpmBar;
    public Slider rpmDecayBar; // Barra que muestra el decay gradual
    public Text rpmText;
    public Image rpmFillImage;

    [Header("Attack Charges")]
    public Image[] attackChargeIcons;
    public Color chargeReadyColor = Color.white;
    public Color chargeEmptyColor = Color.gray;
    public Color chargeRechargingColor = Color.yellow;

    [Header("Special Meter")]
    public Slider specialMeter;
    public Image specialIcon;
    public ParticleSystem specialReadyEffect;

    [Header("Status Effects")]
    public GameObject[] statusEffectIcons;

    private BeyBladeController assignedPlayer;
    private float lastRPMValue;
    private Coroutine rpmDecayCoroutine;

    public void SetupForPlayer(BeyBladeController player)
    {
        assignedPlayer = player;

        if (player?.stats != null)
        {
            // Setup player info
            if (playerNameText != null)
                playerNameText.text = player.stats.beyBladeName;

            if (playerColorImage != null)
                playerColorImage.color = player.stats.bladeColor;

            // Setup RPM bar
            if (rpmBar != null)
            {
                rpmBar.maxValue = player.stats.maxRPM;
                rpmBar.value = player.currentRPM;
            }

            if (rpmDecayBar != null)
            {
                rpmDecayBar.maxValue = player.stats.maxRPM;
                rpmDecayBar.value = player.currentRPM;
            }

            // Setup attack charges
            SetupAttackCharges();

            lastRPMValue = player.currentRPM;
        }
    }

    private void Update()
    {
        if (assignedPlayer != null && !assignedPlayer.isDefeated)
        {
            UpdateRPMDisplay();
            UpdateAttackCharges();
            UpdateSpecialMeter();
            UpdateStatusEffects();
        }
    }

    private void UpdateRPMDisplay()
    {
        float currentRPM = assignedPlayer.currentRPM;
        float maxRPM = assignedPlayer.stats.maxRPM;

        // Update main RPM bar immediately
        if (rpmBar != null)
        {
            rpmBar.value = currentRPM;
        }

        // Update RPM text
        if (rpmText != null)
        {
            rpmText.text = $"{Mathf.RoundToInt(currentRPM)}";
        }

        // Handle decay bar (smooth transition)
        if (currentRPM < lastRPMValue)
        {
            // RPM decreased - start decay animation
            if (rpmDecayCoroutine != null)
                StopCoroutine(rpmDecayCoroutine);

            rpmDecayCoroutine = StartCoroutine(AnimateRPMDecay(lastRPMValue, currentRPM));
        }
        else if (rpmDecayBar != null)
        {
            // RPM increased or stayed same - update decay bar immediately
            rpmDecayBar.value = currentRPM;
        }

        // Update color based on RPM percentage
        UpdateRPMColor(currentRPM / maxRPM);

        lastRPMValue = currentRPM;
    }

    private IEnumerator AnimateRPMDecay(float fromRPM, float toRPM)
    {
        float duration = 1f; // Decay animation duration
        float elapsed = 0f;

        while (elapsed < duration && rpmDecayBar != null)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            rpmDecayBar.value = Mathf.Lerp(fromRPM, toRPM, progress);
            yield return null;
        }

        if (rpmDecayBar != null)
            rpmDecayBar.value = toRPM;
    }

    private void UpdateRPMColor(float rpmPercentage)
    {
        if (rpmFillImage == null) return;

        if (rpmPercentage > 0.6f)
        {
            rpmFillImage.color = Color.Lerp(Color.yellow, Color.green, (rpmPercentage - 0.6f) / 0.4f);
        }
        else if (rpmPercentage > 0.3f)
        {
            rpmFillImage.color = Color.Lerp(Color.red, Color.yellow, (rpmPercentage - 0.3f) / 0.3f);
        }
        else
        {
            rpmFillImage.color = Color.red;
        }
    }

    private void SetupAttackCharges()
    {
        if (attackChargeIcons == null || assignedPlayer?.stats == null) return;

        int maxCharges = assignedPlayer.stats.maxAttackCharges;

        for (int i = 0; i < attackChargeIcons.Length; i++)
        {
            if (attackChargeIcons[i] != null)
            {
                attackChargeIcons[i].gameObject.SetActive(i < maxCharges);
            }
        }
    }

    private void UpdateAttackCharges()
    {
        if (attackChargeIcons == null || assignedPlayer == null) return;

        float currentCharges = assignedPlayer.currentAttackCharges;
        int fullCharges = Mathf.FloorToInt(currentCharges);
        float partialCharge = currentCharges - fullCharges;

        for (int i = 0; i < attackChargeIcons.Length; i++)
        {
            if (attackChargeIcons[i] != null && i < assignedPlayer.stats.maxAttackCharges)
            {
                if (i < fullCharges)
                {
                    // Full charge
                    attackChargeIcons[i].color = chargeReadyColor;
                }
                else if (i == fullCharges && partialCharge > 0)
                {
                    // Recharging
                    attackChargeIcons[i].color = Color.Lerp(chargeEmptyColor, chargeRechargingColor, partialCharge);
                }
                else
                {
                    // Empty
                    attackChargeIcons[i].color = chargeEmptyColor;
                }
            }
        }
    }

    private void UpdateSpecialMeter()
    {
        if (specialMeter == null || assignedPlayer == null) return;

        specialMeter.value = assignedPlayer.specialMeterCharge;

        // Show special ready effect
        if (specialReadyEffect != null)
        {
            if (assignedPlayer.specialMeterCharge >= 1f && !specialReadyEffect.isPlaying)
            {
                specialReadyEffect.Play();
            }
            else if (assignedPlayer.specialMeterCharge < 1f && specialReadyEffect.isPlaying)
            {
                specialReadyEffect.Stop();
            }
        }

        // Pulse special icon when ready
        if (specialIcon != null && assignedPlayer.specialMeterCharge >= 1f)
        {
            float pulse = (Mathf.Sin(Time.time * 4f) + 1f) / 2f;
            specialIcon.color = Color.Lerp(Color.white, Color.yellow, pulse);
        }
        else if (specialIcon != null)
        {
            specialIcon.color = Color.white;
        }
    }

    private void UpdateStatusEffects()
    {
        // Update status effect icons based on player state
        if (statusEffectIcons == null || assignedPlayer == null) return;

        // Example: Show invulnerability icon
        if (statusEffectIcons.Length > 0 && statusEffectIcons[0] != null)
        {
            statusEffectIcons[0].SetActive(assignedPlayer.isInvulnerable);
        }

        // Example: Show special active icon
        if (statusEffectIcons.Length > 1 && statusEffectIcons[1] != null)
        {
            statusEffectIcons[1].SetActive(assignedPlayer.isSpecialActive);
        }

        // Example: Show low RPM warning
        if (statusEffectIcons.Length > 2 && statusEffectIcons[2] != null)
        {
            float rpmPercentage = assignedPlayer.currentRPM / assignedPlayer.stats.maxRPM;
            statusEffectIcons[2].SetActive(rpmPercentage < 0.25f);
        }
    }

    // Called when player is defeated
    public void OnPlayerDefeated()
    {
        if (rpmBar != null) rpmBar.value = 0;
        if (rpmDecayBar != null) rpmDecayBar.value = 0;
        if (rpmText != null) rpmText.text = "DEFEATED";
        if (specialMeter != null) specialMeter.value = 0;

        // Gray out attack charges
        for (int i = 0; i < attackChargeIcons.Length; i++)
        {
            if (attackChargeIcons[i] != null)
            {
                attackChargeIcons[i].color = Color.gray;
            }
        }

        // Stop special effects
        if (specialReadyEffect != null && specialReadyEffect.isPlaying)
        {
            specialReadyEffect.Stop();
        }
    }
}