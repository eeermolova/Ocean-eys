using UnityEngine;

public class HorizontalBackground : MonoBehaviour
{
    [Header("Настройки движения")]
    [Tooltip("Скорость движения фона")]
    public float speed = 3f;

    [Tooltip("Ширина фона в юнитах (500px / 100 = 5)")]
    public float backgroundWidth = 5f;

    [Header("Автоматическая настройка")]
    [Tooltip("Включить автонастройку позиции телепортации")]
    public bool autoConfigure = true;

    [Tooltip("Смещение точки телепортации (1.0 = на ширину фона)")]
    [Range(0.5f, 2.0f)]
    public float teleportOffset = 1.5f;

    [Header("Системные")]
    [Tooltip("Точка телепортации (автоматически или вручную)")]
    public GameObject teleportPoint;

    // Приватные переменные
    private float teleportXPosition;

    void Start()
    {
        // Автоматическая настройка
        if (autoConfigure)
        {
            ConfigureAutomatically();
        }
        else if (teleportPoint != null)
        {
            // Используем ручную настройку
            teleportXPosition = teleportPoint.transform.position.x;
        }

        Debug.Log($"{gameObject.name}: телепорт на X={teleportXPosition}");
    }

    void Update()
    {
        // Движение ВЛЕВО по X
        MoveLeft();

        // Проверка телепортации
        CheckForTeleport();
    }

    void MoveLeft()
    {
        // Движение влево с постоянной скоростью
        transform.Translate(Vector3.left * speed * Time.deltaTime);
    }

    void CheckForTeleport()
    {
        // Если фон прошел точку телепортации
        if (transform.position.x < teleportXPosition)
        {
            TeleportBackground();
        }
    }

    void TeleportBackground()
    {
        // Телепортируем фон вправо (на 2 ширины для плавности)
        float teleportDistance = backgroundWidth * 2f;
        float newX = transform.position.x + teleportDistance;

        transform.position = new Vector3(
            newX,
            transform.position.y,
            transform.position.z
        );

        Debug.Log($"{gameObject.name} телепортирован на X={newX}");
    }

    void ConfigureAutomatically()
    {
        // Автоматический расчет точки телепортации
        // Для двух фонов телепортируем когда пройдено 1.5 ширины
        teleportXPosition = transform.position.x - (backgroundWidth * teleportOffset);

        // Создаем визуальный маркер если нужно
        if (teleportPoint == null)
        {
            CreateTeleportMarker();
        }
    }

    void CreateTeleportMarker()
    {
        GameObject marker = new GameObject($"{gameObject.name}_TeleportMarker");
        marker.transform.position = new Vector3(teleportXPosition, transform.position.y, 0);
        teleportPoint = marker;
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            float tempTeleportX = transform.position.x - (backgroundWidth * teleportOffset);

            // Линия от фона до точки телепортации
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, new Vector3(tempTeleportX, transform.position.y, 0));

            // Точка телепортации
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(tempTeleportX, transform.position.y, 0), 0.3f);

            // Границы фона
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x + backgroundWidth / 2, transform.position.y, 0),
                new Vector3(backgroundWidth, 2.5f, 0)
            );
        }
    }
}