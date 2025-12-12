using UnityEngine;
using System.Collections.Generic;

public class BackgroundWithTopClouds : MonoBehaviour
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
        // Находим главную камеру
        mainCamera = Camera.main;

        // Инициализируем фон
        InitializeBackground();

        // Настраиваем слой отрисовки для фона
        SetupBackgroundSorting();

        // Устанавливаем случайное время для первой проверки спавна облаков
        nextSpawnCheckTime = Time.time + Random.Range(0.5f, 2f);

        // Выводим сообщение в консоль для отладки
        Debug.Log("Система фона и облаков запущена. Доступно спрайтов облаков: " + (cloudSprites != null ? cloudSprites.Length : 0));
    }

    void InitializeBackground()
    {
        // Получаем все дочерние объекты (части фона)
        backgroundParts = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            backgroundParts[i] = transform.GetChild(i);
        }

        // Если частей фона меньше 2, выводим предупреждение
        if (backgroundParts.Length < 2)
        {
            Debug.LogWarning("Нужно как минимум 2 части фона для зацикливания!");
            return;
        }

        // Сортируем части фона по позиции X (от меньшей к большей)
        System.Array.Sort(backgroundParts, (a, b) => a.position.x.CompareTo(b.position.x));

        // Устанавливаем начальные индексы
        leftIndex = 0;                     // Самый левый фон
        rightIndex = backgroundParts.Length - 1; // Самый правый фон
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
        // Проверяем наличие необходимых компонентов
        if (mainCamera == null || backgroundParts == null || backgroundParts.Length < 2)
            return;

        // Обновляем границы камеры
        UpdateCameraBounds();

        // Двигаем фон
        MoveBackground();

        // Проверяем и переставляем части фона при необходимости
        CheckBackgroundReposition();

        // Управляем облаками
        ManageClouds();
    }

    void UpdateCameraBounds()
    {
        // Рассчитываем правую границу камеры
        float cameraHeight = mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        cameraRightEdge = mainCamera.transform.position.x + (cameraWidth / 2);
    }

    void MoveBackground()
    {
        // Двигаем весь фон (родительский объект)
        transform.Translate(Vector3.left * backgroundSpeed * Time.deltaTime);
    }

    void CheckBackgroundReposition()
    {
        // Получаем самую левую часть фона
        Transform leftmost = backgroundParts[leftIndex];

        // Рассчитываем левую границу камеры
        float cameraLeftEdge = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;

        // Рассчитываем правый край самой левой части фона
        float backgroundRightEdge = leftmost.position.x + backgroundWidth / 2;

        // Если фон полностью ушел за левую границу камеры
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
        // Двигаем все облака
        MoveClouds();

        // Удаляем облака, которые ушли за экран
        DestroyOffscreenClouds();

        // Пытаемся создать новое облако
        TrySpawnCloud();
    }

    void MoveClouds()
    {
        // Двигаем каждое облако с его собственной скоростью
        for (int i = 0; i < activeClouds.Count; i++)
        {
            if (activeClouds[i] != null)
            {
                activeClouds[i].transform.Translate(
                    Vector3.left * cloudSpeeds[i] * Time.deltaTime);
            }
        }
    }

    void DestroyOffscreenClouds()
    {
        // Проходим по списку облаков с конца (чтобы не сломать индексы при удалении)
        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            // Если облако было уничтожено другим способом
            if (activeClouds[i] == null)
            {
                activeClouds.RemoveAt(i);
                cloudSpeeds.RemoveAt(i);
                continue;
            }

            // Проверяем, ушло ли облако за левый край экрана
            float cloudX = activeClouds[i].transform.position.x;
            float cameraLeftEdge = mainCamera.transform.position.x -
                                  mainCamera.orthographicSize * mainCamera.aspect - 10f;

            if (cloudX < cameraLeftEdge)
            {
                // Уничтожаем облако
                Destroy(activeClouds[i]);
                activeClouds.RemoveAt(i);
                cloudSpeeds.RemoveAt(i);
            }
        }
    }

    void TrySpawnCloud()
    {
        // Проверяем время - можно ли сейчас проверять спавн
        if (Time.time < nextSpawnCheckTime) return;

        // Проверяем максимальное количество облаков
        if (activeClouds.Count >= maxCloudsOnScreen) return;

        // Проверяем наличие спрайтов облаков
        if (cloudSprites == null || cloudSprites.Length == 0) return;

        // Рандомный шанс появления облака
        if (Random.value > spawnChance) return;

        // Создаем новое облако
        SpawnSingleCloud();

        // Устанавливаем время следующей проверки
        nextSpawnCheckTime = Time.time + Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    void SpawnSingleCloud()
    {
        // 1. Выбираем случайный спрайт из 4 доступных
        int spriteIndex = Random.Range(0, cloudSprites.Length);
        Sprite cloudSprite = cloudSprites[spriteIndex];

        // 2. Генерируем случайную скорость для облака
        float speed = Random.Range(minCloudSpeed, maxCloudSpeed);

        // 3. Генерируем случайную высоту (основная высота + разброс)
        float height = cloudSpawnHeight + Random.Range(-heightVariation, heightVariation);

        // 4. Устанавливаем позицию спавна (правее видимой области камеры)
        Vector3 spawnPos = new Vector3(
            cameraRightEdge + Random.Range(3f, 8f), // Немного правее экрана
            height,
            0
        );

        // 5. Создаем новый игровой объект для облака
        GameObject cloud = new GameObject("Cloud_" + Time.time);
        cloud.transform.position = spawnPos;

        // 6. Добавляем компонент SpriteRenderer для отрисовки
        SpriteRenderer renderer = cloud.AddComponent<SpriteRenderer>();
        renderer.sprite = cloudSprite;

        // 7. НАСТРАИВАЕМ СЛОИ ОТРИСОВКИ - КРИТИЧЕСКИ ВАЖНО!
        //    Облака должны быть ПОВЕРХ фона
        renderer.sortingLayerName = cloudSortingLayer;
        renderer.sortingOrder = cloudOrderInLayer; // Это число должно быть БОЛЬШЕ чем у фона!

        // 8. Устанавливаем случайный размер облака
        float scale = Random.Range(minCloudScale, maxCloudScale);
        cloud.transform.localScale = new Vector3(scale, scale, 1);

        // 9. Настраиваем прозрачность облака
        Color color = renderer.color;
        color.a = Random.Range(0.8f, 1f); // Почти непрозрачные (0.8-1.0)
        renderer.color = color;

        // 10. Добавляем облако в списки для управления
        activeClouds.Add(cloud);
        cloudSpeeds.Add(speed);

        // 11. Выводим информацию для отладки
        Debug.Log($"Создано облако #{spriteIndex + 1}, скорость: {speed:F1}, высота: {height:F1}");
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ОТЛАДКИ ===

    void OnDrawGizmosSelected()
    {
        // Рисуем линию высоты спавна облаков в редакторе
        if (mainCamera != null)
        {
            // Основная линия высоты спавна
            Gizmos.color = Color.cyan;
            float left = mainCamera.transform.position.x - mainCamera.orthographicSize * mainCamera.aspect;
            float right = mainCamera.transform.position.x + mainCamera.orthographicSize * mainCamera.aspect;
            Vector3 start = new Vector3(left, cloudSpawnHeight, 0);
            Vector3 end = new Vector3(right, cloudSpawnHeight, 0);
            Gizmos.DrawLine(start, end);

            // Область разброса высоты (пунктирные линии)
            Gizmos.color = Color.cyan * 0.5f;
            Gizmos.DrawLine(new Vector3(left, cloudSpawnHeight - heightVariation, 0),
                          new Vector3(right, cloudSpawnHeight - heightVariation, 0));
            Gizmos.DrawLine(new Vector3(left, cloudSpawnHeight + heightVariation, 0),
                          new Vector3(right, cloudSpawnHeight + heightVariation, 0));
        }
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ИЗ ДРУГИХ СКРИПТОВ ===

    /// <summary>
    /// Немедленно создать одно облако
    /// </summary>
    public void SpawnCloudNow()
    {
        if (cloudSprites != null && cloudSprites.Length > 0)
            SpawnSingleCloud();
    }

    /// <summary>
    /// Удалить все облака
    /// </summary>
    public void ClearAllClouds()
    {
        foreach (GameObject cloud in activeClouds)
        {
            if (cloud != null) Destroy(cloud);
        }
        activeClouds.Clear();
        cloudSpeeds.Clear();
        Debug.Log("Все облака удалены");
    }

    /// <summary>
    /// Изменить настройки облаков во время работы
    /// </summary>
    public void ChangeCloudSettings(float newMinSpeed, float newMaxSpeed, float newSpawnChance)
    {
        minCloudSpeed = Mathf.Max(0.1f, newMinSpeed);
        maxCloudSpeed = Mathf.Max(minCloudSpeed, newMaxSpeed);
        spawnChance = Mathf.Clamp01(newSpawnChance);

        Debug.Log($"Настройки облаков изменены: скорость {minCloudSpeed}-{maxCloudSpeed}, шанс {spawnChance}");
    }

    /// <summary>
    /// Изменить количество облаков на экране
    /// </summary>
    public void SetMaxClouds(int newMax)
    {
        maxCloudsOnScreen = Mathf.Max(1, newMax);

        // Если сейчас облаков больше нового максимума, удаляем лишние
        while (activeClouds.Count > maxCloudsOnScreen)
        {
            if (activeClouds[0] != null)
                Destroy(activeClouds[0]);
            activeClouds.RemoveAt(0);
            cloudSpeeds.RemoveAt(0);
        }

        Debug.Log($"Максимальное количество облаков изменено на: {maxCloudsOnScreen}");
    }

    /// <summary>
    /// Получить количество активных облаков
    /// </summary>
    public int GetActiveCloudsCount()
    {
        return activeClouds.Count;
    }
}