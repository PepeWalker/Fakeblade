using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("Battle Settings")]
    public float battlePrepTime = 3f;
    public bool enableTeamBattle = false;
    public int teamSize = 2;

    [Header("Battle Modes")]
    public BattleMode currentBattleMode = BattleMode.FreeForAll;

    [Header("Effects")]
    public ParticleSystem battleStartEffect;
    public ParticleSystem battleEndEffect;

    private List<BeyBladeController> activePlayers = new List<BeyBladeController>();
    private bool isBattleActive = false;
    private float battleStartTime;

    public enum BattleMode
    {
        FreeForAll,
        LastManStanding,
        TeamBattle,
        Survival,
        TimeAttack
    }

    public void StartBattle(List<BeyBladeController> players)
    {
        if (players == null || players.Count < 2)
        {
            Debug.LogError("Need at least 2 players to start battle!");
            return;
        }

        activePlayers = new List<BeyBladeController>(players);
        isBattleActive = true;
        battleStartTime = Time.time;

        // Initialize battle mode
        InitializeBattleMode();

        // Play battle start effects
        PlayBattleStartEffects();

        // Subscribe to player defeat events
        BeyBladeController.OnBeyBladeDefeated += OnPlayerDefeated;

        Debug.Log($"Battle started with {activePlayers.Count} players in {currentBattleMode} mode");
    }

    public void EndBattle()
    {
        isBattleActive = false;

        // Unsubscribe from events
        BeyBladeController.OnBeyBladeDefeated -= OnPlayerDefeated;

        // Play battle end effects
        PlayBattleEndEffects();

        // Stop all player actions
        foreach (var player in activePlayers)
        {
            if (player != null)
            {
                player.GetComponent<BeyBladePhysics>()?.SetDefeated();
            }
        }

        Debug.Log("Battle ended");
    }

    private void InitializeBattleMode()
    {
        switch (currentBattleMode)
        {
            case BattleMode.FreeForAll:
                InitializeFreeForAll();
                break;
            case BattleMode.TeamBattle:
                InitializeTeamBattle();
                break;
            case BattleMode.Survival:
                InitializeSurvivalMode();
                break;
            case BattleMode.TimeAttack:
                InitializeTimeAttack();
                break;
        }
    }

    private void InitializeFreeForAll()
    {
        // Standard free-for-all setup - no special initialization needed
        Debug.Log("Free-for-all battle initialized");
    }

    private void InitializeTeamBattle()
    {
        if (enableTeamBattle && activePlayers.Count >= 4)
        {
            // Assign teams
            for (int i = 0; i < activePlayers.Count; i++)
            {
                // Simple team assignment: alternate players
                int teamIndex = i % 2;
                // You could add team assignment logic here
                Debug.Log($"{activePlayers[i].name} assigned to team {teamIndex + 1}");
            }
        }
    }

    private void InitializeSurvivalMode()
    {
        // In survival mode, players face waves of AI enemies
        // This would require spawning AI-controlled beyblades
        Debug.Log("Survival mode initialized");
    }

    private void InitializeTimeAttack()
    {
        // Time attack mode - specific time-based rules
        if (GameManager.Instance != null)
        {
            GameManager.Instance.enableTimeLimit = true;
        }
        Debug.Log("Time attack mode initialized");
    }

    private void OnPlayerDefeated(BeyBladeController defeatedPlayer)
    {
        if (!isBattleActive) return;

        Debug.Log($"{defeatedPlayer.name} was defeated in battle!");

        // Check if battle should continue based on mode
        CheckBattleEndConditions();
    }

    private void CheckBattleEndConditions()
    {
        if (!isBattleActive) return;

        switch (currentBattleMode)
        {
            case BattleMode.FreeForAll:
            case BattleMode.LastManStanding:
                CheckLastManStandingCondition();
                break;
            case BattleMode.TeamBattle:
                CheckTeamBattleCondition();
                break;
            case BattleMode.Survival:
                CheckSurvivalCondition();
                break;
        }
    }

    private void CheckLastManStandingCondition()
    {
        var alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            BeyBladeController winner = alivePlayers.Count == 1 ? alivePlayers[0] : null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndBattle(winner);
            }
        }
    }

    private void CheckTeamBattleCondition()
    {
        // Check if all players from one team are defeated
        // This would require proper team tracking implementation
        var alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= activePlayers.Count / 2)
        {
            // Simplified: if half or fewer players remain, end battle
            BeyBladeController winner = alivePlayers.Count > 0 ? alivePlayers[0] : null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndBattle(winner);
            }
        }
    }

    private void CheckSurvivalCondition()
    {
        var alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count == 0)
        {
            // All human players defeated
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndBattle(null);
            }
        }
    }

    private List<BeyBladeController> GetAlivePlayers()
    {
        List<BeyBladeController> alive = new List<BeyBladeController>();

        foreach (var player in activePlayers)
        {
            if (player != null && !player.isDefeated)
            {
                alive.Add(player);
            }
        }

        return alive;
    }

    private void PlayBattleStartEffects()
    {
        if (battleStartEffect != null)
        {
            battleStartEffect.Play();
        }

        // Could add screen effects, camera shake, etc.
        StartCoroutine(BattleStartSequence());
    }

    private void PlayBattleEndEffects()
    {
        if (battleEndEffect != null)
        {
            battleEndEffect.Play();
        }
    }

    private IEnumerator BattleStartSequence()
    {
        // Dramatic battle start sequence
        yield return new WaitForSeconds(0.5f);

        // Could add camera movements, special effects, etc.

        Debug.Log("Battle start sequence completed");
    }

    public float GetBattleDuration()
    {
        return isBattleActive ? Time.time - battleStartTime : 0f;
    }

    public int GetAlivePlayerCount()
    {
        return GetAlivePlayers().Count;
    }

    public List<BeyBladeController> GetActivePlayers()
    {
        return new List<BeyBladeController>(activePlayers);
    }

    public bool IsBattleActive()
    {
        return isBattleActive;
    }
}