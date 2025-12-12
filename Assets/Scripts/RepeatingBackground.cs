using UnityEngine;

public class RepeatingBackground : MonoBehaviour
{
    // === НАСТРОЙКИ ===
    [Header("Настройки движения")]
    [Tooltip("Скорость движения фона")]
    public float scrollSpeed = 5f;

    [Tooltip("Автоматически определить ширину")]
    public bool autoDetectWidth = true;

    [Header("Ручная настройка")]
    [Tooltip("Ширина одного фона (если autoDetectWidth = false)")]
    public float backgroundWidth = 20f;

    [Tooltip("Количество фонов должно быть 2 или 3")]
    public int backgroundCount = 3;

    // === ПЕРЕМЕННЫЕ ===
    private Transform[] backgrounds;
    private int currentLeftIndex = 0;
    private int currentRightIndex = 0;
    private Camera mainCamera;

    void Start()
    {
        // Находим главную камеру
        mainCamera = Camera.main;

        // Инициализируем массив фонов
        InitializeBackgrounds();

        // Настраиваем начальные индексы
        SetupIndices();
    }

    void InitializeBackgrounds()
    {
        // Получаем все дочерние объекты (фоны)
        backgrounds = new Transform[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            backgrounds[i] = transform.GetChild(i);
        }

        // Если фонов меньше 2, выводим предупреждение
        if (backgrounds.Length < 2)
        {
            Debug.LogWarning("Нужно как минимум 2 фона для повторения!");
            return;
        }

        // Автоматически определяем ширину фона
        if (autoDetectWidth && backgrounds.Length > 0)
        {
            SpriteRenderer spriteRenderer = backgrounds[0].GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                backgroundWidth = spriteRenderer.bounds.size.x;
                Debug.Log($"Автоматически определена ширина фона: {backgroundWidth}");
            }
        }
    }

    void SetupIndices()
    {
        // Сортируем фоны по позиции X (от меньшего к большему)
        System.Array.Sort(backgrounds, (a, b) =>
            a.position.x.CompareTo(b.position.x));

        // Левый индекс - самый левый фон
        currentLeftIndex = 0;
        // Правый индекс - самый правый фон
        currentRightIndex = backgrounds.Length - 1;
    }

    void Update()
    {
        // Если нет камеры или фонов - выходим
        if (mainCamera == null || backgrounds == null || backgrounds.Length < 2)
            return;

        // Двигаем все фоны
        MoveBackgrounds();

        // Проверяем, нужно ли переставить фон
        CheckAndRepositionBackground();
    }

    void MoveBackgrounds()
    {
        // Двигаем родительский объект (все фоны вместе)
        Vector3 moveAmount = Vector3.left * scrollSpeed * Time.deltaTime;
        transform.Translate(moveAmount);
    }

    void CheckAndRepositionBackground()
    {
        // Получаем самый левый фон
        Transform leftmostBackground = backgrounds[currentLeftIndex];

        // Если левый фон полностью ушел за левую границу камеры
        float cameraLeftEdge = mainCamera.transform.position.x -
                              mainCamera.orthographicSize * mainCamera.aspect;

        float backgroundRightEdge = leftmostBackground.position.x + backgroundWidth / 2;

        if (backgroundRightEdge < cameraLeftEdge)
        {
            // Перемещаем левый фон в правую сторону
            RepositionBackground(currentLeftIndex);

            // Обновляем индексы
            UpdateIndices();
        }
    }

    void RepositionBackground(int backgroundIndex)
    {
        // Получаем самый правый фон
        Transform rightmostBackground = backgrounds[currentRightIndex];

        // Позиция для перемещения = справа от самого правого фона
        Vector3 newPosition = rightmostBackground.position;
        newPosition.x += backgroundWidth;

        // Перемещаем фон
        backgrounds[backgroundIndex].position = newPosition;
    }

    void UpdateIndices()
    {
        // Перемещаем индексы по кругу
        currentRightIndex = currentLeftIndex;
        currentLeftIndex = (currentLeftIndex + 1) % backgrounds.Length;
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ОТЛАДКИ ===

    void OnDrawGizmosSelected()
    {
        // Рисуем границы фонов для наглядности
        if (backgrounds != null)
        {
            foreach (Transform bg in backgrounds)
            {
                if (bg != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(bg.position,
                        new Vector3(backgroundWidth, 10f, 1f));
                }
            }
        }
    }
}