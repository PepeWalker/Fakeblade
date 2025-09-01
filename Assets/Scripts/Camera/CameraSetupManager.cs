using UnityEngine;
using System.Collections.Generic;

public class CameraSetupManager : MonoBehaviour
{
    [Header("Main Camera")]
    public Camera mainCamera;
    public Transform cameraTarget; // Empty GameObject to follow multiple targets

    [Header("Camera Settings")]
    public float cameraHeight = 10f;
    public float cameraDistance = 15f;
    public float followSpeed = 2f;
    public float lookSpeed = 3f;
    public Vector3 cameraOffset = new Vector3(0, 8, -12);

    [Header("Multi-Camera Setup")]
    public bool enableSplitScreen = false;
    public int maxPlayersForSplitScreen = 4;
    public Camera[] playerCameras = new Camera[4];

    [Header("Arena Focus")]
    public Transform arenaCenter;
    public float arenaRadius = 10f;
    public bool keepArenaInView = true;

    [Header("Camera Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.3f;

    private List<BeyBladeController> trackedPlayers = new List<BeyBladeController>();
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private bool isInitialized = false;

    // Camera shake variables
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private Vector3 originalPosition;

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (isInitialized)
        {
            UpdateCameraTarget();
            UpdateCameraPosition();
            UpdateCameraShake();
        }
    }

    public void Initialize()
    {
        // Get main camera if not assigned
        if (mainCamera == null)
        {
            // First try to get the camera tagged as MainCamera
            mainCamera = Camera.main;

            if (mainCamera == null)
            {
                // If no MainCamera tag exists, find the first active camera
                Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (Camera cam in allCameras)
                {
                    if (cam.isActiveAndEnabled)
                    {
                        mainCamera = cam;
                        break;
                    }
                }

                // If still no camera found, create a warning
                if (mainCamera == null)
                {
                    Debug.LogWarning("No active camera found! Please assign a main camera to CameraSetupManager.");
                }
            }
        }

        // Create camera target if it doesn't exist
        if (cameraTarget == null)
        {
            GameObject targetGO = new GameObject("Camera Target");
            cameraTarget = targetGO.transform;
        }

        SetupMainCamera();

        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        isInitialized = true;
        Debug.Log("Camera system initialized");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void SetupMainCamera()
    {
        if (mainCamera == null) return;

        // Set initial camera position
        if (arenaCenter != null)
        {
            Vector3 initialPos = arenaCenter.position + cameraOffset;
            mainCamera.transform.position = initialPos;
            mainCamera.transform.LookAt(arenaCenter.position);
            cameraTarget.position = arenaCenter.position;
        }
    }

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.MainMenu:
                SetupMenuCamera();
                break;
            case GameManager.GameState.Battle:
                SetupBattleCamera();
                break;
            case GameManager.GameState.BattleEnd:
                SetupVictoryCamera();
                break;
        }
    }

    private void SetupMenuCamera()
    {
        DisableSplitScreen();

        if (mainCamera != null && arenaCenter != null)
        {
            // Static camera for menu
            Vector3 menuPos = arenaCenter.position + new Vector3(0, 8, -10);
            mainCamera.transform.position = menuPos;
            mainCamera.transform.LookAt(arenaCenter.position);
        }
    }

    private void SetupBattleCamera()
    {
        if (GameManager.Instance?.activePlayers != null)
        {
            trackedPlayers = new List<BeyBladeController>(GameManager.Instance.activePlayers);

            int playerCount = trackedPlayers.Count;

            if (enableSplitScreen && playerCount > 2 && playerCount <= maxPlayersForSplitScreen)
            {
                SetupSplitScreenCameras(playerCount);
            }
            else
            {
                SetupSingleBattleCamera();
            }
        }
    }

    private void SetupSingleBattleCamera()
    {
        DisableSplitScreen();

        if (mainCamera != null)
        {
            mainCamera.rect = new Rect(0, 0, 1, 1);
        }
    }

    private void SetupSplitScreenCameras(int playerCount)
    {
        // Setup split screen rects based on player count
        Rect[] screenRects = GetSplitScreenRects(playerCount);

        // Setup main camera for first player or overview
        if (mainCamera != null)
        {
            mainCamera.rect = screenRects[0];
        }

        // Setup additional cameras for other players
        for (int i = 1; i < playerCount && i < playerCameras.Length + 1; i++)
        {
            Camera cam;

            if (i == 1 && mainCamera != null)
            {
                cam = mainCamera; // Use main camera for first player
            }
            else
            {
                int camIndex = i - 1;
                if (camIndex < playerCameras.Length)
                {
                    cam = playerCameras[camIndex];

                    // Create camera if it doesn't exist
                    if (cam == null)
                    {
                        GameObject camGO = new GameObject($"Player Camera {i}");
                        cam = camGO.AddComponent<Camera>();
                        cam.transform.parent = transform;
                        playerCameras[camIndex] = cam;
                    }

                    cam.enabled = true;
                }
                else
                {
                    continue;
                }
            }

            if (cam != null && i <= screenRects.Length)
            {
                cam.rect = screenRects[i - 1];
            }
        }
    }

    private Rect[] GetSplitScreenRects(int playerCount)
    {
        switch (playerCount)
        {
            case 2:
                return new Rect[]
                {
                    new Rect(0f, 0.5f, 1f, 0.5f), // Top half
                    new Rect(0f, 0f, 1f, 0.5f)    // Bottom half
                };
            case 3:
                return new Rect[]
                {
                    new Rect(0f, 0.5f, 0.5f, 0.5f),   // Top left
                    new Rect(0.5f, 0.5f, 0.5f, 0.5f), // Top right
                    new Rect(0.25f, 0f, 0.5f, 0.5f)   // Bottom center
                };
            case 4:
                return new Rect[]
                {
                    new Rect(0f, 0.5f, 0.5f, 0.5f),   // Top left
                    new Rect(0.5f, 0.5f, 0.5f, 0.5f), // Top right
                    new Rect(0f, 0f, 0.5f, 0.5f),     // Bottom left
                    new Rect(0.5f, 0f, 0.5f, 0.5f)    // Bottom right
                };
            default:
                return new Rect[] { new Rect(0f, 0f, 1f, 1f) };
        }
    }

    private void DisableSplitScreen()
    {
        // Reset main camera
        if (mainCamera != null)
        {
            mainCamera.rect = new Rect(0f, 0f, 1f, 1f);
        }

        // Disable additional cameras
        for (int i = 0; i < playerCameras.Length; i++)
        {
            if (playerCameras[i] != null)
            {
                playerCameras[i].enabled = false;
                playerCameras[i].rect = new Rect(0f, 0f, 1f, 1f);
            }
        }
    }

    private void SetupVictoryCamera()
    {
        DisableSplitScreen();

        // Focus on winner or center of arena
        BeyBladeController winner = null;

        if (GameManager.Instance?.activePlayers != null)
        {
            foreach (var player in GameManager.Instance.activePlayers)
            {
                if (player != null && !player.isDefeated)
                {
                    winner = player;
                    break;
                }
            }
        }

        if (winner != null && cameraTarget != null)
        {
            // Focus on winner
            cameraTarget.position = winner.transform.position;
        }
        else if (arenaCenter != null && cameraTarget != null)
        {
            // Focus on arena center
            cameraTarget.position = arenaCenter.position;
        }
    }

    private void UpdateCameraTarget()
    {
        if (cameraTarget == null || trackedPlayers.Count == 0) return;

        // Calculate center position of all alive players
        Vector3 center = Vector3.zero;
        int aliveCount = 0;
        float maxDistance = 0f;

        foreach (var player in trackedPlayers)
        {
            if (player != null && !player.isDefeated)
            {
                center += player.transform.position;
                aliveCount++;
            }
        }

        if (aliveCount > 0)
        {
            center /= aliveCount;

            // Calculate how spread out the players are
            foreach (var player in trackedPlayers)
            {
                if (player != null && !player.isDefeated)
                {
                    float distance = Vector3.Distance(center, player.transform.position);
                    if (distance > maxDistance)
                        maxDistance = distance;
                }
            }

            // Adjust camera distance based on spread
            float dynamicDistance = Mathf.Clamp(cameraDistance + maxDistance, cameraDistance * 0.5f, cameraDistance * 2f);

            // Keep camera focused on arena if enabled
            if (keepArenaInView && arenaCenter != null)
            {
                float distanceFromArena = Vector3.Distance(center, arenaCenter.position);
                if (distanceFromArena > arenaRadius * 0.5f)
                {
                    Vector3 directionToArena = (arenaCenter.position - center).normalized;
                    center = Vector3.Lerp(center, arenaCenter.position + directionToArena * arenaRadius * 0.3f, 0.5f);
                }
            }

            // Smooth target movement
            targetPosition = center;
            targetPosition.y = Mathf.Max(center.y + 2f, cameraTarget.position.y); // Keep some height

            cameraTarget.position = Vector3.SmoothDamp(cameraTarget.position, targetPosition, ref currentVelocity, 1f / followSpeed);
        }
    }

    private void UpdateCameraPosition()
    {
        if (mainCamera == null || cameraTarget == null) return;

        // Calculate desired camera position
        Vector3 desiredPosition = cameraTarget.position + cameraOffset;

        // Smooth camera movement
        Vector3 smoothedPosition = Vector3.Lerp(mainCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);
        mainCamera.transform.position = smoothedPosition;

        // Smooth camera rotation to look at target
        Vector3 direction = (cameraTarget.position - mainCamera.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, lookRotation, lookSpeed * Time.deltaTime);
    }

    private void UpdateCameraShake()
    {
        if (!isShaking) return;

        shakeTimer -= Time.deltaTime;

        if (shakeTimer <= 0f)
        {
            isShaking = false;
            return;
        }

        // Apply shake effect
        Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity * (shakeTimer / shakeDuration);
        shakeOffset.z = 0; // Don't shake forward/backward too much

        if (mainCamera != null)
        {
            mainCamera.transform.position = originalPosition + shakeOffset;
        }
    }

    #region Public Methods
    public void FocusOnPlayer(BeyBladeController player)
    {
        if (player != null && cameraTarget != null)
        {
            cameraTarget.position = player.transform.position;
        }
    }

    public void FocusOnArenaCenter()
    {
        if (arenaCenter != null && cameraTarget != null)
        {
            cameraTarget.position = arenaCenter.position;
        }
    }

    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        cameraOffset.y = height;
    }

    public void EnableSplitScreen(bool enable)
    {
        enableSplitScreen = enable;

        if (!enable)
        {
            DisableSplitScreen();
        }
        else if (GameManager.Instance?.activePlayers != null)
        {
            SetupSplitScreenCameras(GameManager.Instance.activePlayers.Count);
        }
    }

    public void ShakeCamera(float intensity = -1f, float duration = -1f)
    {
        if (mainCamera == null) return;

        // Use default values if not provided
        if (intensity < 0) intensity = shakeIntensity;
        if (duration < 0) duration = shakeDuration;

        isShaking = true;
        shakeTimer = duration;
        originalPosition = mainCamera.transform.position;

        Debug.Log($"Camera shake: intensity {intensity}, duration {duration}");
    }

    public void SetCameraOffset(Vector3 offset)
    {
        cameraOffset = offset;
    }

    public void SetFollowSpeed(float speed)
    {
        followSpeed = speed;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (cameraTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(cameraTarget.position, 1f);
        }

        if (arenaCenter != null && keepArenaInView)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);
        }
    }
}