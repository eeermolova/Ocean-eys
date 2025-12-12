using UnityEngine;
using System.Collections.Generic;

public class RepeatingBackgroundWithClouds : MonoBehaviour
{
    // === НАСТРОЙКИ ФОНА ===
    [Header("НАСТРОЙКИ ФОНА")]
    [Tooltip("Скорость движения фона")]
    public float backgroundSpeed = 2f;

    [Tooltip("Ширина одного фона")]
    public float backgroundWidth = 19.9565f;

    // === НАСТРОЙКИ ОБЛАКОВ ===
    [Header("НАСТРОЙКИ ОБЛАКОВ")]
    [Tooltip("Спрайты 4-х облаков (перетащите сюда)")]
    public Sprite[] cloudSprites;

    [Tooltip("Минимальная скорость облака")]
    [Range(0.5f, 5f)]
    public float minCloudSpeed = 1f;

    [Tooltip("Максимальная скорость облака")]
    [Range(1f, 10f)]
    public float maxCloudSpeed = 3f;

    [Tooltip("Высота появления облаков")]
    public float cloudSpawnHeight = 3f;

    [Tooltip("Разброс по высоте (+/- от основной)")]
    public float heightVariation = 1.5f;

    [Tooltip("Минимальная задержка между появлением (сек)")]
    public float minSpawnDelay = 1f;

    [Tooltip("Максимальная задержка между появлением (сек)")]
    public float maxSpawnDelay = 4f;

    [Tooltip("Максимальное количество облаков на экране")]
    [Range(1, 10)]
    public int maxCloudsOnScreen = 4;

    [Tooltip("Шанс появления при проверке (0-1)")]
    [Range(0f, 1f)]
    public float spawnChance = 0.5f;

    [Tooltip("Минимальный размер облака")]
    public float minCloudScale = 0.7f;

    [Tooltip("Максимальный размер облака")]
    public float maxCloudScale = 1.3f;

    [Header("СЛОИ ОТРИСОВКИ")]
    [Tooltip("Слой для фона (должен быть НИЖЕ облаков)")]
    public string backgroundSortingLayer = "Default";

    [Tooltip("Порядок в слое для фона")]
    public int backgroundOrderInLayer = 0;

    [Tooltip("Слой для облаков (должен быть ВЫШЕ фона)")]
    public string cloudSortingLayer = "Default";

    [Tooltip("Порядок в слое для облаков")]
    public int cloudOrderInLayer = 1; // БОЛЬШЕ чем у фона!

    // === ПЕРЕМЕННЫЕ ===
    private Transform[] backgroundParts;
    private int leftIndex = 0;
    private int rightIndex = 0;
    private Camera mainCamera;

    private List<GameObject> activeClouds = new List<GameObject>();
    private List<float> cloudSpeeds = new List<float>();
    private float nextSpawnCheckTime;
    private float cameraRightEdge;

    void Start()
    {
        mainCamera = Camera.main;
        InitializeBackground();
        SetupBackgroundSorting(); // Настраиваем слой фона

        nextSpawnCheckTime = Time.time + Random.Range(0.5f, 2f);
        Debug.Log("Система фона и облаков запущена");
    }

    void InitializeBackground()
    {
        // Получаем все части фона
        backgroundParts = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            backgroundParts[i] = transform.GetChild(i);
        }

        // Сортируем по позиции X
        System.Array.Sort(backgroundParts, (a, b) => a.position.x.CompareTo(b.position.x));
        leftIndex = 0;
        rightIndex = backgroundParts.Length - 1;
    }

    void SetupBackgroundSorting()
    {
        // Устанавливаем слой отрисовки для ВСЕХ частей фона
        foreach (Transform part in backgroundParts)
        {
            SpriteRenderer sr = part.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = backgroundSortingLayer;
                sr.sortingOrder = backgroundOrderInLayer; // Меньше чем у облаков
            }
        }
    }

    void Update()
    {
        if (mainCamera == null || backgroundParts == null) return;

        UpdateCameraBounds();
        MoveBackground();
        CheckBackgroundReposition();
        ManageClouds();
    }

    void UpdateCameraBounds()
    {
        float cameraHeight = mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        cameraRightEdge = mainCamera.transform.position.x + (cameraWidth / 2);
    }

    void MoveBackground()
    {
        // Двигаем весь фон
        transform.Translate(Vector3.left * backgroundSpeed * Time.deltaTime);
    }

    void CheckBackgroundReposition()
    {
        Transform leftmost = backgroundParts[leftIndex];
        float cameraLeftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
        float backgroundRightEdge = leftmost.position.x + backgroundWidth / 2;

        if (backgroundRightEdge < cameraLeftEdge)
        {
            // Перемещаем левый фон вправо
            Transform rightmost = backgroundParts[rightIndex];
            Vector3 newPos = rightmost.position;
            newPos.x += backgroundWidth;
            leftmost.position = newPos;

            // Обновляем индексы
            rightIndex = leftIndex;
            leftIndex = (leftIndex + 1) % backgroundParts.Length;
        }
    }

    void ManageClouds()
    {
        MoveClouds();
        DestroyOffscreenClouds();
        TrySpawnCloud();
    }

    void MoveClouds()
    {
        for (int i = 0; i < activeClouds.Count; i++)
        {
            if (activeClouds[i] != null)
            {
                // Каждое облако движется со своей скоростью
                activeClouds[i].transform.Translate(
                    Vector3.left * cloudSpeeds[i] * Time.deltaTime);
            }
        }
    }

    void DestroyOffscreenClouds()
    {
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            if (activeClouds[i] == null)
            {
                activeClouds.RemoveAt(i);
                cloudSpeeds.RemoveAt(i);
                continue;
            }

            // Уничтожаем облака, которые ушли за левый край
            float cloudX = activeClouds[i].transform.position.x;
            float cameraLeftEdge = mainCamera.transform.position.x -
                                  mainCamera.orthographicSize * mainCamera.aspect - 10f;

            if (cloudX < cameraLeftEdge)
            {
                Destroy(activeClouds[i]);
                activeClouds.RemoveAt(i);
                cloudSpeeds.RemoveAt(i);
            }
        }
    }

    void TrySpawnCloud()
    {
        // Проверяем условия
        if (Time.time < nextSpawnCheckTime) return;
        if (activeClouds.Count >= maxCloudsOnScreen) return;
        if (cloudSprites == null || cloudSprites.Length == 0) return;

        // Рандомный шанс
        if (Random.value > spawnChance) return;

        // Спавним облако
        SpawnSingleCloud();

        // Устанавливаем следующую проверку
        nextSpawnCheckTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    void SpawnSingleCloud()
    {
        // 1. Выбираем случайный спрайт из 4
        int spriteIndex = Random.Range(0, cloudSprites.Length);
        Sprite cloudSprite = cloudSprites[spriteIndex];

        // 2. Случайная скорость
        float speed = Random.Range(minCloudSpeed, maxCloudSpeed);

        // 3. Случайная высота
        float height = cloudSpawnHeight + Random.Range(-heightVariation, heightVariation);

        // 4. Позиция спавна (правее экрана)
        Vector3 spawnPos = new Vector3(
            cameraRightEdge + Random.Range(3f, 8f),
            height,
            0
        );

        // 5. Создаем облако
        GameObject cloud = new GameObject("Cloud_" + Time.time);
        cloud.transform.position = spawnPos;

        // 6. Добавляем SpriteRenderer
        SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
        renderer.sprite = cloudSprite;

        // 7. НАСТРОЙКА СЛОЯ - ВАЖНО! Облака ПОВЕРХ фона
        renderer.sortingLayerName = cloudSortingLayer;
        renderer.sortingOrder = cloudOrderInLayer; // БОЛЬШЕ чем у фона!

        // 8. Случайный размер
        float scale = Random.Range(minCloudScale, maxCloudScale);
        cloud.transform.localScale = new Vector3(scale, scale, 1);

        // 9. Случайная прозрачность
        Color color = renderer.color;
        color.a = Random.Range(0.8f, 1f); // Почти непрозрачные
        renderer.color = color;

        // 10. Сохраняем
        activeClouds.Add(cloud);
        cloudSpeeds.Add(speed);

        Debug.Log($"Облако создано: тип={spriteIndex + 1}, скорость={speed:F1}, высота={height:F1}");
    }

    // === МЕТОДЫ ДЛЯ ОТЛАДКИ ===

    void OnDrawGizmosSelected()
    {
        // Рисуем линию высоты спавна облаков
        if (mainCamera != null)
        {
            Gizmos.color = Color.cyan;
            float left = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
            float right = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect;
            Vector3 start = new Vector3(left, cloudSpawnHeight, 0);
            Vector3 end = new Vector3(right, cloudSpawnHeight, 0);
            Gizmos.DrawLine(start, end);

            // Область разброса
            Gizmos.color = Color.cyan * 0.5f;
            Gizmos.DrawLine(new Vector3(left, cloudSpawnHeight - heightVariation, 0),
                          new Vector3(right, cloudSpawnHeight - heightVariation, 0));
            Gizmos.DrawLine(new Vector3(left, cloudSpawnHeight + heightVariation, 0),
                          new Vector3(right, cloudSpawnHeight + heightVariation, 0));
        }
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ===

    public void SpawnCloudNow()
    {
        if (cloudSprites != null && cloudSprites.Length > 0)
            SpawnSingleCloud();
    }

    public void ClearAllClouds()
    {
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null) Destroy(cloud);
        }
        activeClouds.Clear();
        cloudSpeeds.Clear();
    }

    public void ChangeCloudSettings(float newMinSpeed, float newMaxSpeed, float newSpawnChance)
    {
        minCloudSpeed = Mathf.Max(0.1f, newMinSpeed);
        maxCloudSpeed = Mathf.Max(minCloudSpeed, newMaxSpeed);
        spawnChance = Mathf.Clamp01(newSpawnChance);
    }
}