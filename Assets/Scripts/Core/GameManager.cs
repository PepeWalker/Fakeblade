using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameSettings gameSettings;
    public int maxPlayers = 4;
    public int currentPlayers = 2;

    [Header("Battle Settings")]
    public float battleDuration = 180f; // 3 minutes
    public bool enableTimeLimit = false;
    private float battleTimer = 0f;

    [Header("Player Management")]
    public List<BeyBladeController> activePlayers = new List<BeyBladeController>();
    public Transform[] spawnPoints;

    [Header("Managers")]
    public BattleManager battleManager;
    public UIManager uiManager;
    public AudioManager audioManager;
    public InputManager inputManager;
    public ArenaController arenaController;

    [Header("Victory Conditions")]
    public VictoryCondition victoryCondition = VictoryCondition.LastBeyBladeStanding;

    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        PlayerSelection,
        BattlePreparation,
        Battle,
        BattleEnd,
        Paused
    }

    public enum VictoryCondition
    {
        LastBeyBladeStanding,
        TimeLimit,
        FirstToScore,
        TeamElimination
    }

    public GameState currentState = GameState.MainMenu;
    private GameState previousState;
    private bool isPaused = false;

    // Events
    public delegate void GameStateChanged(GameState newState);
    public static event GameStateChanged OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to beyblade defeat events
            BeyBladeController.OnBeyBladeDefeated += OnBeyBladeDefeated;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.Battle:
                UpdateBattleTimer();
                CheckVictoryConditions();
                break;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        BeyBladeController.OnBeyBladeDefeated -= OnBeyBladeDefeated;
    }

    public void InitializeGame()
    {
        // Initialize systems
        if (inputManager != null) inputManager.Initialize(maxPlayers);
        if (audioManager != null) audioManager.Initialize();
        if (uiManager != null) uiManager.Initialize();

        Debug.Log("Game initialized successfully");
    }

    #region Game State Management
    public void ChangeGameState(GameState newState)
    {
        if (currentState == newState) return;

        previousState = currentState;
        currentState = newState;

        HandleGameStateChange(newState);
        OnGameStateChanged?.Invoke(newState);

        if (audioManager != null)
        {
            audioManager.OnGameStateChanged(newState);
        }

        Debug.Log($"Game state changed from {previousState} to {newState}");
    }

    private void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.PlayerSelection:
                HandlePlayerSelectionState();
                break;
            case GameState.BattlePreparation:
                HandleBattlePreparationState();
                break;
            case GameState.Battle:
                HandleBattleState();
                break;
            case GameState.BattleEnd:
                HandleBattleEndState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
        }
    }

    private void HandleMainMenuState()
    {
        Time.timeScale = 1f;
        ClearActivePlayers();
    }

    private void HandlePlayerSelectionState()
    {
        // Prepare for player selection
        Time.timeScale = 1f;
    }

    private void HandleBattlePreparationState()
    {
        // Setup arena, spawn players
        SetupBattle();
    }

    private void HandleBattleState()
    {
        Time.timeScale = 1f;
        battleTimer = 0f;

        if (battleManager != null)
        {
            battleManager.StartBattle(activePlayers);
        }

        if (uiManager != null)
        {
            uiManager.ShowBattleHUD();
        }
    }

    private void HandleBattleEndState()
    {
        Time.timeScale = 1f;

        if (battleManager != null)
        {
            battleManager.EndBattle();
        }
    }

    private void HandlePausedState()
    {
        Time.timeScale = 0f;
    }
    #endregion

    #region Battle Management
    private void SetupBattle()
    {
        SpawnPlayers();

        if (arenaController != null)
        {
            // Register all players with arena
            foreach (var player in activePlayers)
            {
                arenaController.RegisterBeyBlade(player);
            }
        }

        // Start countdown
        StartCoroutine(BattleCountdown());
    }

    private void SpawnPlayers()
    {
        for (int i = 0; i < activePlayers.Count; i++)
        {
            if (spawnPoints != null && i < spawnPoints.Length)
            {
                Vector3 spawnPos = spawnPoints[i].position;
                activePlayers[i].transform.position = spawnPos;
                activePlayers[i].transform.rotation = Quaternion.LookRotation(
                    (arenaController.GetArenaCenter() - spawnPos).normalized);
            }
            else if (arenaController != null)
            {
                // Use random spawn positions if no spawn points defined
                activePlayers[i].transform.position = arenaController.GetRandomArenaPosition(1f);
            }

            // Initialize the beyblade
            activePlayers[i].Initialize();
        }
    }

    private IEnumerator BattleCountdown()
    {
        if (uiManager != null)
        {
            uiManager.ShowCountdown();
        }

        for (int i = 3; i > 0; i--)
        {
            if (audioManager != null)
            {
                audioManager.PlayCountdown();
            }

            if (uiManager != null)
            {
                uiManager.UpdateCountdown(i.ToString());
            }

            yield return new WaitForSeconds(1f);
        }

        if (uiManager != null)
        {
            uiManager.UpdateCountdown("GO!");
        }

        yield return new WaitForSeconds(0.5f);

        if (uiManager != null)
        {
            uiManager.HideCountdown();
        }

        // Start battle
        ChangeGameState(GameState.Battle);
    }

    private void UpdateBattleTimer()
    {
        if (enableTimeLimit)
        {
            battleTimer += Time.deltaTime;

            if (uiManager != null)
            {
                float remainingTime = battleDuration - battleTimer;
                uiManager.UpdateBattleTimer(remainingTime);

                if (remainingTime <= 0)
                {
                    EndBattleByTimeLimit();
                }
            }
        }
    }

    private void CheckVictoryConditions()
    {
        switch (victoryCondition)
        {
            case VictoryCondition.LastBeyBladeStanding:
                CheckLastStandingVictory();
                break;
            case VictoryCondition.TimeLimit:
                // Handled in UpdateBattleTimer
                break;
        }
    }

    private void CheckLastStandingVictory()
    {
        var alivePlayers = activePlayers.FindAll(p => !p.isDefeated);

        if (alivePlayers.Count <= 1)
        {
            BeyBladeController winner = alivePlayers.Count == 1 ? alivePlayers[0] : null;
            EndBattle(winner);
        }
    }

    private void EndBattleByTimeLimit()
    {
        // Find winner by highest RPM
        BeyBladeController winner = null;
        float highestRPM = 0f;

        foreach (var player in activePlayers)
        {
            if (!player.isDefeated && player.currentRPM > highestRPM)
            {
                highestRPM = player.currentRPM;
                winner = player;
            }
        }

        EndBattle(winner);
    }

    private void OnBeyBladeDefeated(BeyBladeController defeatedBlade)
    {
        Debug.Log($"{defeatedBlade.name} was defeated!");

        // Check if battle should end
        if (currentState == GameState.Battle)
        {
            CheckVictoryConditions();
        }
    }
    #endregion

    #region Public Methods
    public void StartBattle()
    {
        if (activePlayers.Count >= 2)
        {
            ChangeGameState(GameState.BattlePreparation);
        }
        else
        {
            Debug.LogError("Need at least 2 players to start battle!");
        }
    }

    public void EndBattle(BeyBladeController winner = null)
    {
        ChangeGameState(GameState.BattleEnd);

        if (uiManager != null)
        {
            uiManager.ShowVictoryScreen(winner);
        }

        Debug.Log(winner != null ? $"{winner.name} wins!" : "Battle ended in a draw!");
    }

    public void RestartBattle()
    {
        // Reset all players
        foreach (var player in activePlayers)
        {
            player.Initialize(); // This will reset RPM and status
        }

        ChangeGameState(GameState.BattlePreparation);
    }

    public void ReturnToMainMenu()
    {
        ClearActivePlayers();
        ChangeGameState(GameState.MainMenu);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Battle)
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                previousState = GameState.Battle;
                ChangeGameState(GameState.Paused);
            }
            else
            {
                ChangeGameState(GameState.Battle);
            }
        }
        else if (currentState == GameState.Paused)
        {
            isPaused = false;
            ChangeGameState(previousState);
        }
    }

    public void RegisterPlayer(BeyBladeController player)
    {
        if (activePlayers.Count < maxPlayers && !activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            player.SetPlayerIndex(activePlayers.Count - 1);

            // Register with input manager
            if (inputManager != null)
            {
                inputManager.RegisterPlayer(player);
            }

            Debug.Log($"Player {activePlayers.Count} registered: {player.name}");
        }
    }

    public void UnregisterPlayer(BeyBladeController player)
    {
        if (activePlayers.Contains(player))
        {
            activePlayers.Remove(player);

            if (inputManager != null)
            {
                inputManager.UnregisterPlayer(player);
            }

            if (arenaController != null)
            {
                arenaController.UnregisterBeyBlade(player);
            }
        }
    }

    private void ClearActivePlayers()
    {
        foreach (var player in activePlayers)
        {
            if (inputManager != null)
            {
                inputManager.UnregisterPlayer(player);
            }

            if (arenaController != null)
            {
                arenaController.UnregisterBeyBlade(player);
            }
        }

        activePlayers.Clear();
    }

    public List<BeyBladeController> GetAlivePlayers()
    {
        return activePlayers.FindAll(p => !p.isDefeated);
    }

    public int GetAlivePlayerCount()
    {
        return activePlayers.Count(p => !p.isDefeated);
    }
    #endregion
}