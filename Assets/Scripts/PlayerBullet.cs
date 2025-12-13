using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    private float damage;
    private LayerMask enemyLayer;
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

    public void Init(Vector2 dir, float speed, float damage, LayerMask enemyLayer, GameObject owner)
    {
        this.damage = damage;
        this.enemyLayer = enemyLayer;
        this.owner = owner;

        if (rb != null)
            rb.linearVelocity = dir.normalized * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.transform.root.gameObject == owner) return;


        // попали во врага (по LayerMask)
        if (((1 << other.gameObject.layer) & enemyLayer.value) != 0)
        {
            Health h = other.GetComponent<Health>();
            if (h != null) h.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // если попали во что-то "твЄрдое" (не триггер) Ч уничтожаем пулю
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
