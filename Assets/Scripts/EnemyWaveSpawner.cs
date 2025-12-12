using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    [Header("Префабы врагов")]
    [SerializeField] private GameObject enemy1Prefab; // твой первый враг (скрипт который ты скинула)
    [SerializeField] private GameObject enemy2Prefab; // второй враг (мой скрипт)

    [Header("Область спавна (2 точки в мире)")]
    [SerializeField] private Transform spawnCornerA; // например верхний правый угол
    [SerializeField] private Transform spawnCornerB; // например нижний правый угол

    [Header("Ограничения")]
    [SerializeField] private int maxAliveEnemies = 3; // ВСЕГДА максимум живых
    [SerializeField] private float spawnInterval = 1.25f;
    [SerializeField] private float initialDelay = 0.5f;

    [Header("Стартовые лимиты (сколько раз заспавнить всего)")]
    [SerializeField] private int enemy1InitialSpawnCount = 10;
    [SerializeField] private int enemy2InitialSpawnCount = 10;

    [Header("Бесконечный режим")]
    [SerializeField] private bool alternateInInfinite = true; // 1->2->1->2...
    [SerializeField] private bool randomInInfinite = false;   // если true — будет рандом вместо очереди

    private readonly List<GameObject> alive = new List<GameObject>();

    private int spawnedEnemy1Total = 0;
    private int spawnedEnemy2Total = 0;

    private enum Phase { Enemy1Wave, Enemy2Wave, Infinite }
    private Phase phase = Phase.Enemy1Wave;

    private bool nextInfiniteIsEnemy1 = true;

    private void Start()
    {
        if (enemy1Prefab == null || enemy2Prefab == null)
        {
            Debug.LogError("[EnemyWaveSpawner] Не назначены префабы enemy1Prefab/enemy2Prefab!");
            enabled = false;
            return;
        }

        if (spawnCornerA == null || spawnCornerB == null)
        {
            Debug.LogError("[EnemyWaveSpawner] Не назначены точки области спавна spawnCornerA/spawnCornerB!");
            enabled = false;
            return;
        }

        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            CleanupAliveList();

            if (alive.Count < maxAliveEnemies)
            {
                // Сначала 10 раз Enemy1
                if (phase == Phase.Enemy1Wave)
                {
                    if (spawnedEnemy1Total < enemy1InitialSpawnCount)
                    {
                        Spawn(enemy1Prefab);
                        spawnedEnemy1Total++;
                    }
                    else
                    {
                        phase = Phase.Enemy2Wave;
                    }
                }
                // Потом 10 раз Enemy2
                else if (phase == Phase.Enemy2Wave)
                {
                    if (spawnedEnemy2Total < enemy2InitialSpawnCount)
                    {
                        Spawn(enemy2Prefab);
                        spawnedEnemy2Total++;
                    }
                    else
                    {
                        phase = Phase.Infinite;
                        nextInfiniteIsEnemy1 = true;
                    }
                }
                // Потом бесконечно (но максимум 3 живых)
                else
                {
                    GameObject prefabToSpawn;

                    if (randomInInfinite)
                    {
                        prefabToSpawn = (Random.value < 0.5f) ? enemy1Prefab : enemy2Prefab;
                    }
                    else if (alternateInInfinite)
                    {
                        prefabToSpawn = nextInfiniteIsEnemy1 ? enemy1Prefab : enemy2Prefab;
                        nextInfiniteIsEnemy1 = !nextInfiniteIsEnemy1;
                    }
                    else
                    {
                        prefabToSpawn = enemy1Prefab; // если выключить всё — будет только Enemy1
                    }

                    Spawn(prefabToSpawn);
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void Spawn(GameObject prefab)
    {
        Vector2 pos = GetRandomPointInSpawnArea();
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        alive.Add(obj);
    }

    private Vector2 GetRandomPointInSpawnArea()
    {
        Vector2 a = spawnCornerA.position;
        Vector2 b = spawnCornerB.position;

        float minX = Mathf.Min(a.x, b.x);
        float maxX = Mathf.Max(a.x, b.x);
        float minY = Mathf.Min(a.y, b.y);
        float maxY = Mathf.Max(a.y, b.y);

        return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
    }

    private void CleanupAliveList()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
                continue;
            }

            // Если твой Health НЕ уничтожает объект при смерти,
            // то мы хотя бы перестанем считать его "живым" для лимита 3
            Health h = alive[i].GetComponent<Health>();
            if (h != null && !h.IsAlive)
            {
                alive.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnCornerA == null || spawnCornerB == null) return;

        Vector2 a = spawnCornerA.position;
        Vector2 b = spawnCornerB.position;

        Vector2 center = (a + b) * 0.5f;
        Vector2 size = new Vector2(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
}
