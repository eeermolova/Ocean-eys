using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 4f;

    private Rigidbody2D rb;
    private float damage;
    private GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    public void Init(Vector2 dir, float speed, float damage, GameObject owner)
    {
        this.damage = damage;
        this.owner = owner;

        if (rb != null)
            rb.linearVelocity = dir.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        if (other.CompareTag("Player"))
        {
            Health h = other.GetComponent<Health>();
            if (h != null) h.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Если попали во что-то “твёрдое” (не триггер) — уничтожаемся
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
