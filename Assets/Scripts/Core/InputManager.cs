using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerInput
    {
        public KeyCode attackKey = KeyCode.Space;
        public KeyCode dashKey = KeyCode.LeftShift;
        public KeyCode specialKey = KeyCode.E;
        public string horizontalAxis = "Horizontal";
        public string verticalAxis = "Vertical";
        public bool useGamepad = false;
        public int gamepadIndex = 0;
    }

    public PlayerInput[] playerInputs = new PlayerInput[4];

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

    public void Initialize(int maxPlayers)
    {
        // Configurar controles por defecto
        SetupDefaultControls(maxPlayers);
    }

    private void SetupDefaultControls(int maxPlayers)
    {
        // Jugador 1 - Teclado
        playerInputs[0] = new PlayerInput
        {
            attackKey = KeyCode.Space,
            dashKey = KeyCode.LeftShift,
            specialKey = KeyCode.E,
            horizontalAxis = "Horizontal",
            verticalAxis = "Vertical"
        };

        // Jugador 2 - Teclado alternativo
        playerInputs[1] = new PlayerInput
        {
            attackKey = KeyCode.Return,
            dashKey = KeyCode.RightShift,
            specialKey = KeyCode.RightControl,
            horizontalAxis = "Horizontal2",
            verticalAxis = "Vertical2"
        };

        // Jugadores 3 y 4 - Gamepads
        for (int i = 2; i < maxPlayers; i++)
        {
            playerInputs[i] = new PlayerInput
            {
                useGamepad = true,
                gamepadIndex = i - 2
            };
        }
    }

    public Vector2 GetMovementInput(int playerIndex)
    {
        if (playerIndex >= playerInputs.Length) return Vector2.zero;

        PlayerInput input = playerInputs[playerIndex];

        if (input.useGamepad)
        {
            return GetGamepadMovement(input.gamepadIndex);
        }
        else
        {
            float horizontal = Input.GetAxis(input.horizontalAxis);
            float vertical = Input.GetAxis(input.verticalAxis);
            return new Vector2(horizontal, vertical);
        }
    }

    public bool GetAttackInput(int playerIndex)
    {
        if (playerIndex >= playerInputs.Length) return false;

        PlayerInput input = playerInputs[playerIndex];

        if (input.useGamepad)
        {
            return Input.GetButtonDown($"joystick {input.gamepadIndex + 1} button 0");
        }
        else
        {
            return Input.GetKeyDown(input.attackKey);
        }
    }

    public bool GetDashInput(int playerIndex)
    {
        if (playerIndex >= playerInputs.Length) return false;

        PlayerInput input = playerInputs[playerIndex];

        if (input.useGamepad)
        {
            return Input.GetButtonDown($"joystick {input.gamepadIndex + 1} button 1");
        }
        else
        {
            return Input.GetKeyDown(input.dashKey);
        }
    }

    public bool GetSpecialInput(int playerIndex)
    {
        if (playerIndex >= playerInputs.Length) return false;

        PlayerInput input = playerInputs[playerIndex];

        if (input.useGamepad)
        {
            return Input.GetButtonDown($"joystick {input.gamepadIndex + 1} button 2");
        }
        else
        {
            return Input.GetKeyDown(input.specialKey);
        }
    }

    private Vector2 GetGamepadMovement(int gamepadIndex)
    {
        string joyName = $"joystick {gamepadIndex + 1}";
        float horizontal = Input.GetAxis($"{joyName} axis 1");
        float vertical = Input.GetAxis($"{joyName} axis 2");
        return new Vector2(horizontal, -vertical); // Invertir Y para gamepads
    }
}