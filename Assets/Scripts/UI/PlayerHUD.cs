using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("Player Info")]
    public int playerIndex = 0;
    public Text playerNameText;
    public Text playerNumberText;
    public Image playerColorFrame;

    [Header("RPM Bar")]
    public Slider rpmBar;
    public Slider rpmDecayBar;
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

    [Header("Rankings")]
    public Text currentRankText;

    private BeyBladeController assignedPlayer;
    private float lastRPMValue;
    private Coroutine rpmDecayCoroutine;

    // Colores por jugador
    private static readonly Color[] PLAYER_COLORS = new Color[]
    {
        Color.blue,    // Jugador 1
        Color.red,     // Jugador 2  
        Color.green,   // Jugador 3
        Color.yellow   // Jugador 4
    };

    public void SetupForPlayer(BeyBladeController player, int index)
    {
        assignedPlayer = player;
        playerIndex = index;

        if (player?.stats != null)
        {
            SetupPlayerInfo();
            SetupRPMSystem();
            SetupAttackCharges();
            lastRPMValue = player.currentRPM;
        }
    }

    private void SetupPlayerInfo()
    {
        if (playerNameText != null)
            playerNameText.text = assignedPlayer.stats.beyBladeName;

        if (playerNumberText != null)
            playerNumberText.text = $"P{playerIndex + 1}";

        if (playerColorFrame != null && playerIndex < PLAYER_COLORS.Length)
            playerColorFrame.color = PLAYER_COLORS[playerIndex];
    }

    private void SetupRPMSystem()
    {
        float maxRPM = assignedPlayer.stats.maxRPM;

        if (rpmBar != null)
        {
            rpmBar.maxValue = maxRPM;
            rpmBar.value = assignedPlayer.currentRPM;
        }

        if (rpmDecayBar != null)
        {
            rpmDecayBar.maxValue = maxRPM;
            rpmDecayBar.value = assignedPlayer.currentRPM;
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

    private void Update()
    {
        if (assignedPlayer != null && !assignedPlayer.isDefeated)
        {
            UpdateRPMDisplay();
            UpdateAttackCharges();
            UpdateSpecialMeter();
            UpdateStatusEffects();
            UpdateRankingInfo();
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
        float duration = 1f;
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
                    attackChargeIcons[i].color = chargeReadyColor;
                }
                else if (i == fullCharges && partialCharge > 0)
                {
                    attackChargeIcons[i].color = Color.Lerp(chargeEmptyColor, chargeRechargingColor, partialCharge);
                }
                else
                {
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

    private void UpdateRankingInfo()
    {
        if (currentRankText == null || GameManager.Instance == null) return;

        int rank = GameManager.Instance.GetPlayerRank(assignedPlayer);
        string rankSuffix = GameManager.Instance.GetPlayerRankSuffix(rank);
        currentRankText.text = $"{rank}{rankSuffix}";
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