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
    public PlayerHUD[] playerHUDs = new PlayerHUD[4];

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

    private void Start()
    {
        SetupButtonListeners();
    }

    public void Initialize()
    {
        ShowMainMenu();
        HideAllPanels();

        // Subscribe to game state changes
        GameManager.OnGameStateChanged += OnGameStateChanged;

        Debug.Log("UIManager initialized");
    }

    private void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void SetupButtonListeners()
    {
        // Main Menu
        if (playButton != null)
            playButton.onClick.AddListener(() => OnPlayButtonClicked());

        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => ShowOptionsMenu());

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
        UpdatePlayerHUDs();
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

    public void HideOptionsMenu()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }
    #endregion

    #region Battle HUD
    private void UpdatePlayerHUDs()
    {
        if (GameManager.Instance?.activePlayers == null) return;

        var activePlayers = GameManager.Instance.activePlayers;

        for (int i = 0; i < playerHUDs.Length; i++)
        {
            if (playerHUDs[i] != null)
            {
                if (i < activePlayers.Count)
                {
                    playerHUDs[i].gameObject.SetActive(true);
                    playerHUDs[i].SetupForPlayer(activePlayers[i]);
                }
                else
                {
                    playerHUDs[i].gameObject.SetActive(false);
                }
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
}