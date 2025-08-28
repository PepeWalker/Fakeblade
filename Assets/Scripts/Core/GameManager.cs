using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public GameSettings gameSettings;
    public int maxPlayers = 4;
    public int currentPlayers = 2;

    [Header("Player Management")]
    public List<BeyBladeController> activePlayers = new List<BeyBladeController>();
    public List<BeyBladeController> defeatedPlayers = new List<BeyBladeController>();


    [Header("Managers")]
    public BattleManager battleManager;
    public UIManager uiManager;
    public AudioManager audioManager;
    public InputManager inputManager;


    [Header("Victory Conditions")]
    public bool teamMode = false;
    public int playersPerTeam = 2;

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


    // Eventos para notificar cambios de estado
    public System.Action<BeyBladeController> OnPlayerDefeatedEvent;
    public System.Action<BeyBladeController> OnPlayerVictoryEvent;
    public System.Action OnBattleEndEvent;

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
<<<<<<< HEAD
        if (inputManager != null) inputManager.Initialize(maxPlayers);
        if (audioManager != null) audioManager.Initialize();
        if (uiManager != null) uiManager.Initialize();


        Debug.Log("Game initialized successfully");
=======
        inputManager.Initialize(maxPlayers);
        audioManager.Initialize();
        uiManager.Initialize();
>>>>>>> parent of 67e279e (cambios en particulas y fisicas)
    }

    public void StartBattle()
    {

        if (activePlayers.Count < 2)
        {
            Debug.LogWarning("No hay suficientes jugadores para iniciar la batalla!");
            return;
        }

        currentState = GameState.Battle;
        defeatedPlayers.Clear();

        // Inicializar stats de todos los jugadores activos
        foreach (var player in activePlayers)
        {
            if (player != null)
            {
                player.Initialize();
            }
        }

        if (battleManager != null) battleManager.StartBattle(activePlayers);
        if (uiManager != null) uiManager.ShowBattleHUD();

        Debug.Log($"Batalla iniciada con {activePlayers.Count} jugadores");
    }

    public void OnPlayerDefeated(BeyBladeController defeatedPlayer)
    {
        if (defeatedPlayer == null || defeatedPlayers.Contains(defeatedPlayer))
            return;

        // Mover jugador a la lista de derrotados
        defeatedPlayers.Add(defeatedPlayer);

        // Invocar evento
        OnPlayerDefeatedEvent?.Invoke(defeatedPlayer);

        Debug.Log($"Jugador {defeatedPlayer.name} ha sido derrotado!");

        // Reproducir efectos de derrota
        if (audioManager != null)
        {
            //audioManager.PlayDefeatSound();
        }

        // Verificar condiciones de victoria
        CheckVictoryConditions();
    }

    private void CheckVictoryConditions()
    {
        if (currentState != GameState.Battle) return;

        List<BeyBladeController> remainingPlayers = activePlayers.Where(p => p != null && !defeatedPlayers.Contains(p) && p.currentRPM > 0).ToList();

        if (teamMode)
        {
            CheckTeamVictory(remainingPlayers);
        }
        else
        {
            CheckFreeForAllVictory(remainingPlayers);
        }
    }

    private void CheckFreeForAllVictory(List<BeyBladeController> remainingPlayers)
    {
        if (remainingPlayers.Count <= 1)
        {
            BeyBladeController winner = remainingPlayers.FirstOrDefault();
            EndBattle(winner);
        }
        else if (remainingPlayers.Count == 0)
        {
            // Empate - nadie sobrevivió
            EndBattle(null);
        }
    }

    private void CheckTeamVictory(List<BeyBladeController> remainingPlayers)
    {
        if (remainingPlayers.Count == 0)
        {
            EndBattle(null); // Empate
            return;
        }

        // Verificar si todos los jugadores restantes son del mismo equipo
        int firstPlayerTeam = remainingPlayers[0].GetPlayerTeam();
        bool sameTeam = remainingPlayers.All(p => p.GetPlayerTeam() == firstPlayerTeam);

        if (sameTeam)
        {
            // El equipo ganador es el del primer jugador restante
            EndBattle(remainingPlayers[0]); // Representante del equipo ganador
        }
    }

    public void EndBattle(BeyBladeController winner)
    {
        if (currentState != GameState.Battle) return;

        currentState = GameState.BattleEnd;

        // Invocar eventos
        OnPlayerVictoryEvent?.Invoke(winner);
        OnBattleEndEvent?.Invoke();

        if (battleManager != null) battleManager.EndBattle();
        if (uiManager != null) uiManager.ShowVictoryScreen(winner);

        if (winner != null)
        {
            Debug.Log($"¡{winner.name} ha ganado la batalla!");

            // Reproducir sonido de victoria
            if (audioManager != null)
            {
                audioManager.PlayVictorySound();
            }
        }
        else
        {
            Debug.Log("¡La batalla terminó en empate!");

            // Reproducir sonido de empate
            if (audioManager != null)
            {
                audioManager.PlayDrawSound();
            }
        }
    }


    public void RegisterPlayer(BeyBladeController player)
    {
        if (player == null) return;

        if (activePlayers.Count < maxPlayers && !activePlayers.Contains(player))
        {
            activePlayers.Add(player);
            player.SetPlayerIndex(activePlayers.Count - 1);

            Debug.Log($"Jugador {player.name} registrado como Player {activePlayers.Count}");
        }
        else
        {
            Debug.LogWarning($"No se pudo registrar el jugador {player.name}. Máximo alcanzado o ya existe.");
        }
    }

    public void UnregisterPlayer(BeyBladeController player)
    {
        if (player == null) return;

        activePlayers.Remove(player);
        defeatedPlayers.Remove(player);

        Debug.Log($"Jugador {player.name} desregistrado");
    }

    public void RestartBattle()
    {
        // Limpiar listas
        defeatedPlayers.Clear();

        // Reinicializar jugadores activos
        foreach (var player in activePlayers)
        {
            if (player != null)
            {
                player.ResetToStartState();
            }
        }

        // Reiniciar batalla
        StartBattle();

        Debug.Log("Batalla reiniciada");
    }



}