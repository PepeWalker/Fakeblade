using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameSettings gameSettings;
    public int maxPlayers = 4;
    public int currentPlayers = 2;

    [Header("Player Management")]
    public List<BeyBladeController> activePlayers = new List<BeyBladeController>();

    [Header("Managers")]
    public BattleManager battleManager;
    public UIManager uiManager;
    public AudioManager audioManager;
    public InputManager inputManager;

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

    public GameState currentState = GameState.MainMenu;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

    public void InitializeGame()
    {
        // Inicializar sistemas
        inputManager.Initialize(maxPlayers);
        audioManager.Initialize();
        //uiManager.Initialize();
    }

    public void StartBattle()
    {
        currentState = GameState.Battle;
        battleManager.StartBattle(activePlayers);
        uiManager.ShowBattleHUD();
    }

    public void EndBattle(BeyBladeController winner)
    {
        currentState = GameState.BattleEnd;
        battleManager.EndBattle();
        uiManager.ShowVictoryScreen(winner);
    }

    public void RegisterPlayer(BeyBladeController player)
    {
        if (activePlayers.Count < maxPlayers)
        {
            activePlayers.Add(player);
            player.SetPlayerIndex(activePlayers.Count - 1);
        }
    }

    public void UnregisterPlayer(BeyBladeController player)
    {
        activePlayers.Remove(player);
    }
}