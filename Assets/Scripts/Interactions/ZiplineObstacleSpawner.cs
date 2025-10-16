using System.Collections.Generic;
using UnityEngine;

public class ZiplineObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [Tooltip("Prefab for obstacles that spawn ON the zipline")]
    [SerializeField] private GameObject onZiplineObstaclePrefab;

    [Tooltip("Prefab for obstacles that spawn BETWEEN ziplines")]
    [SerializeField] private GameObject betweenZiplinesObstaclePrefab;

    [Header("On-Zipline Obstacles")]
    [Tooltip("Minimum time between obstacle spawns on ziplines")]
    [SerializeField] private float onZiplineMinSpawnInterval = 3f;

    [Tooltip("Maximum time between obstacle spawns on ziplines")]
    [SerializeField] private float onZiplineMaxSpawnInterval = 6f;

    [Tooltip("Hang offset for on-zipline obstacles")]
    [SerializeField] private Vector3 onZiplineHangOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Between-Zipline Obstacles")]
    [Tooltip("Minimum time between obstacle spawns between ziplines")]
    [SerializeField] private float betweenZiplinesMinSpawnInterval = 4f;

    [Tooltip("Maximum time between obstacle spawns between ziplines")]
    [SerializeField] private float betweenZiplinesMaxSpawnInterval = 8f;

    [Tooltip("Hang offset for between-zipline obstacles")]
    [SerializeField] private Vector3 betweenZiplinesHangOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Random offset from center (0.5). Example: 0.2 means obstacles spawn randomly between 0.3 and 0.7")]
    [Range(0f, 0.5f)]
    [SerializeField] private float betweenZiplinesRandomness = 0.2f;

    [Header("Zipline References")]
    [Tooltip("All ziplines in the system that can spawn obstacles")]
    [SerializeField] private List<Zipline> ziplines = new List<Zipline>();

    [Header("Spawn Control")]
    [Tooltip("Enable obstacle spawning")]
    [SerializeField] private bool enableSpawning = true;

    private float _nextOnZiplineSpawnTime;
    private float _nextBetweenZiplinesSpawnTime;
    private Transform _obstacleContainer;
    private RespawnScript _templateRespawnScript;

    private void Start()
    {
        _obstacleContainer = new GameObject("Zipline Obstacles").transform;

        CacheTemplateRespawnScript();
        ResetSpawnTimers();
    }

    private void CacheTemplateRespawnScript()
    {
        GameObject respawning = GameObject.Find("Respawning");
        if (respawning != null)
        {
            Transform respawnCollider = respawning.transform.Find("RespawnCollider");
            if (respawnCollider != null)
            {
                _templateRespawnScript = respawnCollider.GetComponent<RespawnScript>();
            }
        }

        if (_templateRespawnScript == null)
        {
            _templateRespawnScript = FindFirstObjectByType<RespawnScript>();
        }
    }

    private void SetupRespawnScript(GameObject obstacle)
    {
        if (_templateRespawnScript == null) return;

        RespawnScript[] respawnScripts = obstacle.GetComponentsInChildren<RespawnScript>();
        foreach (RespawnScript respawn in respawnScripts)
        {
            respawn.player = _templateRespawnScript.player;
            respawn.respawnPoint = _templateRespawnScript.respawnPoint;
        }
    }

    private void Update()
    {
        if (!enableSpawning) return;

        if (Time.time >= _nextOnZiplineSpawnTime && onZiplineObstaclePrefab != null)
        {
            TrySpawnOnZiplineObstacle();
            _nextOnZiplineSpawnTime = Time.time + Random.Range(onZiplineMinSpawnInterval, onZiplineMaxSpawnInterval);
        }

        if (Time.time >= _nextBetweenZiplinesSpawnTime && betweenZiplinesObstaclePrefab != null)
        {
            TrySpawnBetweenZiplinesObstacle();
            _nextBetweenZiplinesSpawnTime = Time.time + Random.Range(betweenZiplinesMinSpawnInterval, betweenZiplinesMaxSpawnInterval);
        }
    }

    private void TrySpawnOnZiplineObstacle()
    {
        if (ziplines.Count == 0)
        {
            Debug.LogWarning("[ZiplineObstacleSpawner] No ziplines in list!");
            return;
        }

        // Gets indicies [0, 2, 4] out of the 6 ziplines, so can reorder ziplines in Inspector to change up which ziplines get obstacles spawned on them
        int evenIndex = Random.Range(0, (ziplines.Count + 1) / 2) * 2;
        Zipline targetZipline = ziplines[evenIndex];

        if (targetZipline == null)
        {
            Debug.LogWarning("[ZiplineObstacleSpawner] Selected zipline is null!");
            return;
        }

        if (targetZipline.targetZip == null)
        {
            Debug.LogWarning($"[ZiplineObstacleSpawner] Zipline {targetZipline.name} has no targetZip!");
            return;
        }

        Vector3 startPos = targetZipline.targetZip.zipTransform.position;
        Vector3 endPos = targetZipline.zipTransform.position;

        GameObject obstacle = Instantiate(onZiplineObstaclePrefab, _obstacleContainer);
        SetupRespawnScript(obstacle);

        ZiplineObstacle obstacleScript = obstacle.GetComponent<ZiplineObstacle>();

        if (obstacleScript != null)
        {
            obstacleScript.Initialize(startPos, endPos, onZiplineHangOffset);
        }
        else
        {
            Debug.LogError("[ZiplineObstacleSpawner] Obstacle prefab missing ZiplineObstacle component!");
            Destroy(obstacle);
        }
    }

    private void TrySpawnBetweenZiplinesObstacle()
    {
        List<(Zipline left, Zipline right)> parallelPairs = FindParallelZiplinePairs();

        Debug.Log(parallelPairs);

        if (parallelPairs.Count == 0)
        {
            Debug.Log("[ZiplineObstacleSpawner] No parallel zipline pairs found");
            return;
        }

        var pair = parallelPairs[Random.Range(0, parallelPairs.Count)];

        Vector3 leftStart = pair.left.targetZip.zipTransform.position;
        Vector3 leftEnd = pair.left.zipTransform.position;
        Vector3 rightStart = pair.right.targetZip.zipTransform.position;
        Vector3 rightEnd = pair.right.zipTransform.position;

        // Can change the directly below line of code to be either value in the future rather than a range
        // float randomOffset = Random.Range(-betweenZiplinesRandomness, betweenZiplinesRandomness);
        float randomOffset = (Random.value < 0.5f) ? -betweenZiplinesRandomness : betweenZiplinesRandomness;
        float lerpFactor = 0.5f + randomOffset;

        Vector3 startPos = Vector3.Lerp(leftStart, rightStart, lerpFactor);
        Vector3 endPos = Vector3.Lerp(leftEnd, rightEnd, lerpFactor);

        GameObject obstacle = Instantiate(betweenZiplinesObstaclePrefab, _obstacleContainer);
        SetupRespawnScript(obstacle);

        ZiplineObstacle obstacleScript = obstacle.GetComponent<ZiplineObstacle>();

        if (obstacleScript != null)
        {
            obstacleScript.Initialize(startPos, endPos, betweenZiplinesHangOffset);
        }
        else
        {
            Debug.LogError("[ZiplineObstacleSpawner] Obstacle prefab missing ZiplineObstacle component!");
            Destroy(obstacle);
        }
    }

    private List<(Zipline left, Zipline right)> FindParallelZiplinePairs()
    {
        List<(Zipline, Zipline)> pairs = new List<(Zipline, Zipline)>();

        foreach (var zipline in ziplines)
        {
            if (zipline == null) continue;

            if (zipline.leftParallelZipline != null && !pairs.Contains((zipline.leftParallelZipline, zipline)))
            {
                pairs.Add((zipline, zipline.leftParallelZipline));
            }
            if (zipline.rightParallelZipline != null && !pairs.Contains((zipline, zipline.rightParallelZipline)))
            {
                pairs.Add((zipline, zipline.rightParallelZipline));
            }
        }

        return pairs;
    }

    private void ResetSpawnTimers()
    {
        _nextOnZiplineSpawnTime = Time.time + Random.Range(onZiplineMinSpawnInterval, onZiplineMaxSpawnInterval);
        _nextBetweenZiplinesSpawnTime = Time.time + Random.Range(betweenZiplinesMinSpawnInterval, betweenZiplinesMaxSpawnInterval);
    }
}