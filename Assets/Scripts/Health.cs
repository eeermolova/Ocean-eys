using UnityEngine;

// Простейший скрипт здоровья без визуальных эффектов
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;  // Максимальное здоровье

    private float currentHealth;  // Текущее здоровье

    // Свойства для доступа к значениям здоровья
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;

    // Метод Start вызывается при создании объекта
    private void Start()
    {
        currentHealth = maxHealth;  // Устанавливаем стартовое здоровье
    }

    // Основной метод для получения урона
    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;  // Если уже мертв - ничего не делаем

        currentHealth -= damage;  // Уменьшаем здоровье

        // Выводим в консоль для отладки
        Debug.Log($"{gameObject.name} получил {damage} урона. Осталось здоровья: {currentHealth}");

        // Проверяем смерть
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Метод лечения
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // Метод смерти
    private void Die()
    {
        Debug.Log($"{gameObject.name} умер");
        Destroy(gameObject);  // Просто уничтожаем объект
    }
}
