using UnityEngine;

// Простейший скрипт здоровья без визуальных эффектов
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;  // Максимальное здоровье
    
    private float currentHealth;  // Текущее здоровье

    // Свойства для доступа к значениям здоровья
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive { get; private set; } = true;

    public event System.Action<float> Damaged;
    public event System.Action Died;

    private bool diedSent = false; // чтобы Died вызвался 1 раз

    // Метод Start вызывается при создании объекта
    private void Start()
    {
        currentHealth = maxHealth;  // Устанавливаем стартовое здоровье
        IsAlive = true;

    }

    // Основной метод для получения урона
    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            IsAlive = false;

            if (!diedSent)
            {
                diedSent = true;
                Died?.Invoke();
            }
            return; // важно: не вызываем Damaged после смерти
        }

        Damaged?.Invoke(damage);

        Debug.Log($"{gameObject.name} получил {damage} урона. Осталось здоровья: {currentHealth}");

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
