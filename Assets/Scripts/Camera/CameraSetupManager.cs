using UnityEngine;

public class CameraSetupManager : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform cameraTransform;
    public bool setupOnStart = true;

    [Header("Player Management")]
    public ImprovedBeyBladeController[] players;

    private void Start()
    {
        if (setupOnStart)
        {
            SetupCameraForAllPlayers();
        }
    }

    public void SetupCameraForAllPlayers()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }

        // Configurar referencia de cámara para todos los jugadores
        ImprovedBeyBladeController[] allControllers = FindObjectsOfType<ImprovedBeyBladeController>();

        foreach (var controller in allControllers)
        {
            controller.SetCameraReference(cameraTransform);
            controller.SetCameraRelativeMovement(true);
        }

        Debug.Log($"Camera setup completed for {allControllers.Length} players");
    }

    public void AddPlayer(ImprovedBeyBladeController newPlayer)
    {
        newPlayer.SetCameraReference(cameraTransform);
        newPlayer.SetCameraRelativeMovement(true);
    }
}