using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCombat : MonoBehaviour
{
    // Основные настройки (видны в инспекторе)
    [SerializeField] private float attackCooldown = 0.5f;  // Задержка между атаками
    [SerializeField] private float attackDamage = 20f;     // Урон за атаку
    [SerializeField] private float attackRange = 1f;       // Дальность атаки

    [SerializeField] private Transform attackPoint;        // Точка, откуда бьем
    [SerializeField] private LayerMask enemyLayer;         // Слой врагов

    // Переменные для Input System
    private InputSystem_Actions actions;
    private InputAction attackAction;

    // Состояние
    private bool canAttack = true;
    private float lastAttackTime = 0f;

    // Метод Awake вызывается при создании объекта
    private void Awake()
    {
        actions = new InputSystem_Actions();
    }

    // При включении объекта
    private void OnEnable()
    {
        actions.Player.Enable();
        actions.Player.Attack.performed += Attack;  // Подписываемся на событие
    }

    // При выключении объекта
    private void OnDisable()
    {
        actions.Player.Attack.performed -= Attack;  // Отписываемся
        attackAction.Disable();              // Отключаем
    }

    // Обработчик нажатия кнопки атаки
    private void Attack(InputAction.CallbackContext context)
    {
        // Если можем атаковать - атакуем
        if (canAttack)
        {
            Attack();
        }
    }

    // Основной метод атаки
    private void Attack()
    {
        // Проверяем кулдаун
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Обновляем время последней атаки
        lastAttackTime = Time.time;

        // Ищем врагов в радиусе
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,  // Центр поиска
            attackRange,           // Радиус поиска
            enemyLayer             // Ищем только врагов
        );

        // Наносим урон всем найденным врагам
        foreach (Collider2D enemy in hitEnemies)
        {
            // Пытаемся получить компонент Health у врага
            Health enemyHealth = enemy.GetComponent<Health>();

            // Если у врага есть здоровье - наносим урон
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
        }

        // Включаем кулдаун
        canAttack = false;

        // Через время attackCooldown снова разрешаем атаку
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    // Сброс состояния атаки
    private void ResetAttack()
    {
        canAttack = true;
    }

    // Метод для отрисовки радиуса атаки в редакторе
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        // Рисуем красный круг - радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
