using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button playButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Battle HUD")]
    public GameObject battleHUDPanel;

    [Header("Player HUD Management")]
    public PlayerHUD[] playerHUDs = new PlayerHUD[4];
    public GameObject[] hudContainers = new GameObject[4]; // Contenedores para cada HUD
    public CanvasGroup[] hudCanvasGroups = new CanvasGroup[4]; // Para fade in/out

    [Header("Battle Timer")]
    public Text battleTimerText;
    public Slider battleTimerSlider;

    [Header("Countdown")]
    public GameObject countdownPanel;
    public Text countdownText;

    [Header("Victory Screen")]
    public GameObject victoryPanel;
    public Text winnerText;
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Pause Menu")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseOptionsButton;
    public Button pauseMainMenuButton;

    [Header("Options Menu")]
    public GameObject optionsPanel;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Button backButton;

    [Header("HUD Layouts")]
    public RectTransform hudParent;
    public bool useResponsiveLayout = true;

    [Header("Additional Battle UI")] // NUEVOS ELEMENTOS
    public GameObject miniMapPanel;
    public Text alivePlayersCountText;
    public Text battleStatusText;
    public GameObject[] powerUpIndicators = new GameObject[4];

    private void Start()
    {
        SetupButtonListeners();
    }

    public void Initialize()
    {
        ShowMainMenu();
        HideAllPanels();

        // Suscribirse al evento
        GameManager.OnGameStateChanged += OnGameStateChanged;

        Debug.Log("UIManager initialized");
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void SetupButtonListeners()
    {
        // Main Menu
        if (playButton != null)
            playButton.onClick.AddListener(() => OnPlayButtonClicked());

        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => ShowOptionsMenu());

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => ShowCredits());

        if (exitButton != null)
            exitButton.onClick.AddListener(() => OnExitButtonClicked());

        // Victory Screen
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(() => OnPlayAgainButtonClicked());

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnMainMenuButtonClicked());

        // Pause Menu
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => OnResumeButtonClicked());

        if (pauseOptionsButton != null)
            pauseOptionsButton.onClick.AddListener(() => ShowOptionsFromPause());

        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(() => OnMainMenuButtonClicked());

        // Options
        if (backButton != null)
            backButton.onClick.AddListener(() => HideOptionsMenu());

        SetupVolumeSliders();
    }

    private void SetupVolumeSliders()
    {
        if (masterVolumeSlider != null && AudioManager.Instance != null)
        {
            masterVolumeSlider.value = AudioManager.Instance.masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        }

        if (musicVolumeSlider != null && AudioManager.Instance != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.musicVolume;
            musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
        }

        if (sfxVolumeSlider != null && AudioManager.Instance != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.sfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
        }
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameManager.GameState.Battle:
                ShowBattleHUD();
                break;
            case GameManager.GameState.BattleEnd:
                // Victory screen will be shown by GameManager calling ShowVictoryScreen
                break;
            case GameManager.GameState.Paused:
                ShowPauseMenu();
                break;
        }
    }

    #region Panel Management
    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (battleHUDPanel != null) battleHUDPanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (miniMapPanel != null) miniMapPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowBattleHUD()
    {
        HideAllPanels();
        if (battleHUDPanel != null) battleHUDPanel.SetActive(true);
        if (miniMapPanel != null) miniMapPanel.SetActive(true);

        SetupPlayerHUDs();
        UpdateBattleStatus();
    }

    public void ShowVictoryScreen(BeyBladeController winner)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            if (winnerText != null)
            {
                if (winner != null)
                {
                    winnerText.text = $"{winner.stats.beyBladeName}\nWINS!";
                }
                else
                {
                    winnerText.text = "DRAW!";
                }
            }
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ShowOptionsMenu()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void ShowOptionsFromPause()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void HideOptionsMenu()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowCredits()
    {
        // Implementar pantalla de créditos
        Debug.Log("Showing credits...");
    }
    #endregion

    #region Player HUD Management - TU CÓDIGO EXISTENTE MEJORADO
    public void SetupPlayerHUDs()
    {
        if (GameManager.Instance?.activePlayers == null) return;

        var activePlayers = GameManager.Instance.activePlayers;
        int playerCount = activePlayers.Count;

        // Configurar cada HUD de jugador
        for (int i = 0; i < 4; i++)
        {
            bool shouldShowHUD = i < playerCount;

            // Activar/desactivar contenedor
            if (hudContainers[i] != null)
                hudContainers[i].SetActive(shouldShowHUD);

            // Configurar HUD si hay jugador
            if (shouldShowHUD && playerHUDs[i] != null)
            {
                playerHUDs[i].SetupForPlayer(activePlayers[i], i);

                // Fade in suave
                if (hudCanvasGroups[i] != null)
                {
                    StartCoroutine(FadeInHUD(hudCanvasGroups[i], 0.5f));
                }
            }
            else if (hudCanvasGroups[i] != null)
            {
                // Fade out para HUDs no utilizados
                StartCoroutine(FadeOutHUD(hudCanvasGroups[i], 0.3f));
            }
        }

        // Ajustar layout responsivo
        if (useResponsiveLayout)
        {
            AdjustHUDLayout(playerCount);
        }
    }

    private void AdjustHUDLayout(int playerCount)
    {
        // Ajustar tamaño y posición según número de jugadores
        float scale = playerCount <= 2 ? 1.0f : 0.8f;

        for (int i = 0; i < playerCount; i++)
        {
            if (playerHUDs[i] != null)
            {
                RectTransform hudRect = playerHUDs[i].GetComponent<RectTransform>();
                if (hudRect != null)
                {
                    hudRect.localScale = Vector3.one * scale;
                }
            }
        }
    }

    private System.Collections.IEnumerator FadeInHUD(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private System.Collections.IEnumerator FadeOutHUD(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
    #endregion

    #region Battle HUD Updates
    private void Update()
    {
        // Actualizar información de batalla en tiempo real
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Battle)
        {
            UpdateBattleStatus();
        }
    }

    private void UpdateBattleStatus()
    {
        if (GameManager.Instance == null) return;

        // Actualizar contador de jugadores vivos
        if (alivePlayersCountText != null)
        {
            int aliveCount = GameManager.Instance.GetAlivePlayerCount();
            alivePlayersCountText.text = $"Players Alive: {aliveCount}";
        }

        // Actualizar estado de batalla
        if (battleStatusText != null)
        {
            if (GameManager.Instance.enableTimeLimit)
            {
                float remainingTime = GameManager.Instance.battleDuration - Time.time;
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                battleStatusText.text = $"Time: {minutes:00}:{seconds:00}";
            }
            else
            {
                battleStatusText.text = "Last BeyBlade Standing";
            }
        }
    }

    public void UpdateBattleTimer(float remainingTime)
    {
        if (battleTimerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            battleTimerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (battleTimerSlider != null && GameManager.Instance != null)
        {
            battleTimerSlider.value = remainingTime / GameManager.Instance.battleDuration;
        }
    }
    #endregion

    #region Countdown
    public void ShowCountdown()
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }
    }

    public void UpdateCountdown(string text)
    {
        if (countdownText != null)
        {
            countdownText.text = text;

            // Efecto de escala para el countdown
            StartCoroutine(CountdownPulseEffect());
        }
    }

    private IEnumerator CountdownPulseEffect()
    {
        if (countdownText != null)
        {
            Vector3 originalScale = countdownText.transform.localScale;

            // Agrandar
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.5f, elapsed / duration);
                countdownText.transform.localScale = originalScale * scale;
                yield return null;
            }

            // Volver al tamaño normal
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1.5f, 1f, elapsed / duration);
                countdownText.transform.localScale = originalScale * scale;
                yield return null;
            }

            countdownText.transform.localScale = originalScale;
        }
    }

    public void HideCountdown()
    {
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }
    #endregion

    #region Power-Up Indicators - NUEVO
    public void ShowPowerUpIndicator(int playerIndex, string powerUpName, float duration)
    {
        if (playerIndex >= 0 && playerIndex < powerUpIndicators.Length && powerUpIndicators[playerIndex] != null)
        {
            powerUpIndicators[playerIndex].SetActive(true);

            Text powerUpText = powerUpIndicators[playerIndex].GetComponentInChildren<Text>();
            if (powerUpText != null)
            {
                powerUpText.text = powerUpName;
            }

            StartCoroutine(HidePowerUpIndicatorAfterDelay(playerIndex, duration));
        }
    }

    private IEnumerator HidePowerUpIndicatorAfterDelay(int playerIndex, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (playerIndex >= 0 && playerIndex < powerUpIndicators.Length && powerUpIndicators[playerIndex] != null)
        {
            powerUpIndicators[playerIndex].SetActive(false);
        }
    }
    #endregion

    #region Button Callbacks
    private void OnPlayButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.BattlePreparation);
        }
    }

    private void OnPlayAgainButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartBattle();
        }
    }

    private void OnMainMenuButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
        }
    }

    private void OnResumeButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    private void OnExitButtonClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region Utility Methods - NUEVOS
    public void ShowNotification(string message, float duration = 3f)
    {
        // Crear notificación temporal
        GameObject notification = new GameObject("Notification");
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            notification.transform.SetParent(canvas.transform, false);
        }

        Text notificationText = notification.AddComponent<Text>();
        notificationText.text = message;
        notificationText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        notificationText.fontSize = 24;
        notificationText.color = Color.yellow;
        notificationText.alignment = TextAnchor.MiddleCenter;

        RectTransform rect = notification.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.sizeDelta = new Vector2(400, 50);

        StartCoroutine(RemoveNotificationAfterDelay(notification, duration));
    }

    private IEnumerator RemoveNotificationAfterDelay(GameObject notification, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (notification != null)
        {
            Destroy(notification);
        }
    }

    public void FlashScreen(Color color, float duration = 0.5f)
    {
        StartCoroutine(ScreenFlashCoroutine(color, duration));
    }

    private IEnumerator ScreenFlashCoroutine(Color color, float duration)
    {
        GameObject flashOverlay = new GameObject("ScreenFlash");
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            flashOverlay.transform.SetParent(canvas.transform, false);
        }

        Image flashImage = flashOverlay.AddComponent<Image>();
        flashImage.color = color;

        RectTransform rect = flashOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
            flashImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        Destroy(flashOverlay);
    }
    #endregion
}