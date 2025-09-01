using UnityEngine;
using System.Collections.Generic;

public class ArenaController : MonoBehaviour
{
    [Header("Arena Settings")]
    public float arenaRadius = 10f;
    public Transform arenaCenter;
    public LayerMask beybladeLayer = -1;

    [Header("Boundary System")]
    public bool enableKnockout = true;
    public float knockoutHeight = -2f;
    public float bounceForce = 500f;
    public float damageOnBoundaryHit = 50f;

    [Header("Special Zones")]
    public List<ArenaZone> specialZones = new List<ArenaZone>();

    [Header("Visual Effects")]
    public ParticleSystem boundaryHitEffect;
    public AudioClip boundaryHitSound;

    [Header("Power-ups")]
    public bool enablePowerUps = false;
    public GameObject[] powerUpPrefabs;
    public float powerUpSpawnInterval = 30f;
    public int maxPowerUpsOnField = 2;

    private List<GameObject> activePowerUps = new List<GameObject>();
    private float lastPowerUpSpawn = 0f;
    private List<BeyBladeController> registeredBlades = new List<BeyBladeController>();

    private void Start()
    {
        InitializeArena();
    }

    private void Update()
    {
        CheckBoundaries();
        UpdateSpecialZones();

        if (enablePowerUps)
        {
            HandlePowerUpSpawning();
        }
    }

    private void InitializeArena()
    {
        if (arenaCenter == null)
            arenaCenter = transform;

        // Initialize special zones
        foreach (var zone in specialZones)
        {
            if (zone != null)
                zone.Initialize();
        }

        Debug.Log($"Arena initialized with radius {arenaRadius}");
    }

    private void CheckBoundaries()
    {
        // Find all BeyBlades in the scene
        BeyBladeController[] allBlades = FindObjectsOfType<BeyBladeController>();

        foreach (var blade in allBlades)
        {
            if (blade.isDefeated) continue;

            float distanceFromCenter = Vector3.Distance(blade.transform.position, arenaCenter.position);
            float heightFromArena = blade.transform.position.y;

            // Check horizontal boundaries
            if (distanceFromCenter > arenaRadius)
            {
                HandleBoundaryCollision(blade, BoundaryType.Wall);
            }

            // Check knockout (fall through)
            if (enableKnockout && heightFromArena < knockoutHeight)
            {
                HandleKnockout(blade);
            }
        }
    }

    private void HandleBoundaryCollision(BeyBladeController blade, BoundaryType boundaryType)
    {
        Vector3 directionFromCenter = (blade.transform.position - arenaCenter.position).normalized;
        Vector3 bounceDirection = directionFromCenter;
        bounceDirection.y = 0.2f; // Small upward component

        // Apply bounce force
        Rigidbody bladeRb = blade.GetComponent<Rigidbody>();
        if (bladeRb != null)
        {
            bladeRb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
        }

        // Apply damage
        blade.ModifyRPM(-damageOnBoundaryHit);

        // Visual and audio effects
        Vector3 hitPosition = arenaCenter.position + directionFromCenter * arenaRadius;
        PlayBoundaryHitEffects(hitPosition);

        // Reposition blade inside arena
        Vector3 safePosition = arenaCenter.position + directionFromCenter * (arenaRadius - 0.5f);
        safePosition.y = blade.transform.position.y;
        blade.transform.position = safePosition;

        Debug.Log($"{blade.name} hit arena boundary and bounced back");
    }

    private void HandleKnockout(BeyBladeController blade)
    {
        blade.SetDefeated();

        // Dramatic knockout effects
        if (boundaryHitEffect != null)
        {
            boundaryHitEffect.transform.position = blade.transform.position;
            boundaryHitEffect.Play();
        }

        if (AudioManager.Instance != null && boundaryHitSound != null)
        {
            AudioManager.Instance.PlaySFX(boundaryHitSound, 0.8f);
        }

        Debug.Log($"{blade.name} was knocked out of the arena!");
    }

    private void UpdateSpecialZones()
    {
        foreach (var zone in specialZones)
        {
            if (zone != null && zone.isActive)
            {
                zone.UpdateZone();
            }
        }
    }

    private void PlayBoundaryHitEffects(Vector3 position)
    {
        if (boundaryHitEffect != null)
        {
            boundaryHitEffect.transform.position = position;
            boundaryHitEffect.Play();
        }

        if (AudioManager.Instance != null && boundaryHitSound != null)
        {
            AudioManager.Instance.PlaySFXAtPosition(boundaryHitSound, position, 0.6f);
        }
    }

    #region Power-Up System
    private void HandlePowerUpSpawning()
    {
        if (Time.time - lastPowerUpSpawn < powerUpSpawnInterval) return;
        if (activePowerUps.Count >= maxPowerUpsOnField) return;
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;

        SpawnRandomPowerUp();
        lastPowerUpSpawn = Time.time;
    }

    private void SpawnRandomPowerUp()
    {
        // Choose random power-up
        GameObject prefab = powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];

        // Choose random position within arena
        Vector2 randomCircle = Random.insideUnitCircle * (arenaRadius * 0.8f);
        Vector3 spawnPosition = arenaCenter.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

        // Spawn power-up
        GameObject powerUp = Instantiate(prefab, spawnPosition, Quaternion.identity);
        activePowerUps.Add(powerUp);

        Debug.Log($"Power-up spawned at {spawnPosition}");
    }

    public void RemovePowerUp(GameObject powerUp)
    {
        if (activePowerUps.Contains(powerUp))
        {
            activePowerUps.Remove(powerUp);
            Destroy(powerUp);
        }
    }
    #endregion

    public void RegisterBeyBlade(BeyBladeController blade)
    {
        if (!registeredBlades.Contains(blade))
        {
            registeredBlades.Add(blade);
        }
    }

    public void UnregisterBeyBlade(BeyBladeController blade)
    {
        if (registeredBlades.Contains(blade))
        {
            registeredBlades.Remove(blade);
        }
    }

    public Vector3 GetArenaCenter()
    {
        return arenaCenter.position;
    }

    public float GetArenaRadius()
    {
        return arenaRadius;
    }

    // Check if position is within arena bounds
    public bool IsPositionInArena(Vector3 position)
    {
        float distance = Vector3.Distance(position, arenaCenter.position);
        return distance <= arenaRadius;
    }

    // Get random position within arena
    public Vector3 GetRandomArenaPosition(float heightOffset = 0f)
    {
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius * 0.9f;
        return arenaCenter.position + new Vector3(randomCircle.x, heightOffset, randomCircle.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (arenaCenter == null) arenaCenter = transform;

        // Draw arena boundary
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(arenaCenter.position, arenaRadius);

        // Draw knockout level
        if (enableKnockout)
        {
            Gizmos.color = Color.yellow;
            Vector3 knockoutCenter = arenaCenter.position;
            knockoutCenter.y = knockoutHeight;
            Gizmos.DrawWireCube(knockoutCenter, new Vector3(arenaRadius * 2, 0.1f, arenaRadius * 2));
        }

        // Draw special zones
        foreach (var zone in specialZones)
        {
            if (zone != null)
            {
                Gizmos.color = zone.GetZoneColor();
                Gizmos.DrawWireSphere(zone.transform.position, zone.GetZoneRadius());
            }
        }
    }
}

public enum BoundaryType
{
    Wall,
    Knockout
}