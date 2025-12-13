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

    public event System.Action Damaged;
    public event System.Action Died;

    private bool diedSent = false; // чтобы Died вызвался 1 раз

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
        if (currentHealth < 0) currentHealth = 0;

        Damaged?.Invoke();

        

        // Выводим в консоль для отладки
        Debug.Log($"{gameObject.name} получил {damage} урона. Осталось здоровья: {currentHealth}");

        // Проверяем смерть
        if (!IsAlive && !diedSent)
        {
            diedSent = true;
            Died?.Invoke();
        }

    }

    // Метод лечения
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    // Метод смерти
    private void Die()
    {
        Debug.Log($"{gameObject.name} умер");
        Destroy(gameObject);  // Просто уничтожаем объект
    }
}
