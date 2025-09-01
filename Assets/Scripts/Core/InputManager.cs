using UnityEngine;
using System.Collections.Generic;

public class InputManager : MonoBehaviour
{
    [Header("Input Settings")]
    public bool allowKeyboardAndMouse = true;
    public bool allowControllers = true;

    [Header("Keyboard Controls")]
    public KeyCode[] player1Keys = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space, KeyCode.LeftShift, KeyCode.Q };
    public KeyCode[] player2Keys = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.RightControl, KeyCode.RightShift, KeyCode.Return };

    private Dictionary<int, PlayerInput> playerInputs = new Dictionary<int, PlayerInput>();
    private List<BeyBladeController> registeredPlayers = new List<BeyBladeController>();

    public struct PlayerInput
    {
        public Vector2 movement;
        public bool attackDown;
        public bool attackHeld;
        public bool dashPressed;
        public bool specialPressed;
        public bool pausePressed;
    }

    public void Initialize(int maxPlayers)
    {
        for (int i = 0; i < maxPlayers; i++)
        {
            playerInputs[i] = new PlayerInput();
        }

        Debug.Log($"InputManager initialized for {maxPlayers} players");
    }

    private void Update()
    {
        UpdateAllPlayerInputs();
        DistributeInputsToPlayers();
    }

    private void UpdateAllPlayerInputs()
    {
        // Update keyboard inputs
        if (allowKeyboardAndMouse)
        {
            UpdateKeyboardInput(0, player1Keys); // Player 1
            if (registeredPlayers.Count > 1)
            {
                UpdateKeyboardInput(1, player2Keys); // Player 2
            }
        }

        // Update controller inputs
        if (allowControllers)
        {
            for (int i = 0; i < 4; i++) // Unity supports up to 4 controllers
            {
                if (i < registeredPlayers.Count)
                {
                    UpdateControllerInput(i);
                }
            }
        }
    }

    private void UpdateKeyboardInput(int playerIndex, KeyCode[] keys)
    {
        if (keys.Length < 7) return;

        PlayerInput input = playerInputs[playerIndex];

        // Movement (WASD or Arrow Keys)
        Vector2 movement = Vector2.zero;
        if (Input.GetKey(keys[0])) movement.y += 1f;  // Forward
        if (Input.GetKey(keys[1])) movement.y -= 1f;  // Backward
        if (Input.GetKey(keys[2])) movement.x -= 1f;  // Left
        if (Input.GetKey(keys[3])) movement.x += 1f;  // Right

        input.movement = movement.normalized;

        // Action buttons
        input.attackDown = Input.GetKeyDown(keys[4]);    // Attack button down
        input.attackHeld = Input.GetKey(keys[4]);        // Attack button held
        input.dashPressed = Input.GetKeyDown(keys[5]);   // Dash
        input.specialPressed = Input.GetKeyDown(keys[6]); // Special
        input.pausePressed = Input.GetKeyDown(KeyCode.Escape);

        playerInputs[playerIndex] = input;
    }

    private void UpdateControllerInput(int playerIndex)
    {
        string controllerPrefix = $"Joystick{playerIndex + 1}";
        PlayerInput input = playerInputs[playerIndex];

        // Left stick movement
        float horizontal = Input.GetAxis($"Horizontal_P{playerIndex + 1}");
        float vertical = Input.GetAxis($"Vertical_P{playerIndex + 1}");
        input.movement = new Vector2(horizontal, vertical);

        // Controller buttons (Xbox controller layout)
        input.attackDown = Input.GetKeyDown($"joystick {playerIndex + 1} button 0"); // A button
        input.attackHeld = Input.GetKey($"joystick {playerIndex + 1} button 0");     // A button held
        input.dashPressed = Input.GetKeyDown($"joystick {playerIndex + 1} button 1"); // B button
        input.specialPressed = Input.GetKeyDown($"joystick {playerIndex + 1} button 2"); // X button
        input.pausePressed = Input.GetKeyDown($"joystick {playerIndex + 1} button 7"); // Start button

        // Alternative: Use Unity's new Input System if available
        // This is fallback for legacy Input Manager

        playerInputs[playerIndex] = input;
    }

    private void DistributeInputsToPlayers()
    {
        for (int i = 0; i < registeredPlayers.Count; i++)
        {
            if (registeredPlayers[i] != null && !registeredPlayers[i].isDefeated)
            {
                PlayerInput input = playerInputs[i];

                registeredPlayers[i].SetMovementInput(input.movement);
                registeredPlayers[i].SetAttackInput(input.attackDown, input.attackHeld);
                registeredPlayers[i].SetDashInput(input.dashPressed);
                registeredPlayers[i].SetSpecialInput(input.specialPressed);

                // Handle pause for any player
                if (input.pausePressed && GameManager.Instance != null)
                {
                    GameManager.Instance.TogglePause();
                }
            }
        }
    }

    public void RegisterPlayer(BeyBladeController player)
    {
        if (!registeredPlayers.Contains(player))
        {
            registeredPlayers.Add(player);
            player.SetPlayerIndex(registeredPlayers.Count - 1);
            Debug.Log($"Player {registeredPlayers.Count} registered for input");
        }
    }

    public void UnregisterPlayer(BeyBladeController player)
    {
        if (registeredPlayers.Contains(player))
        {
            registeredPlayers.Remove(player);
            Debug.Log($"Player unregistered from input");
        }
    }

    public PlayerInput GetPlayerInput(int playerIndex)
    {
        if (playerInputs.ContainsKey(playerIndex))
        {
            return playerInputs[playerIndex];
        }
        return new PlayerInput();
    }

    // Method to check if controller is connected
    public bool IsControllerConnected(int controllerIndex)
    {
        string[] joystickNames = Input.GetJoystickNames();
        return controllerIndex < joystickNames.Length && !string.IsNullOrEmpty(joystickNames[controllerIndex]);
    }

    // Get connected controller count
    public int GetConnectedControllerCount()
    {
        int count = 0;
        string[] joystickNames = Input.GetJoystickNames();
        for (int i = 0; i < joystickNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(joystickNames[i]))
                count++;
        }
        return count;
    }
}