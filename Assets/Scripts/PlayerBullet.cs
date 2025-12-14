using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    private Vector2 dir;
    private float speed;
    private float damage;
    private LayerMask enemyMask;
    private GameObject owner;

    private Rigidbody2D rb;

    [SerializeField] private float lifeTime = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float bulletSpeed, float bulletDamage, LayerMask enemyLayer, GameObject ownerObj)
    {
        dir = direction.normalized;
        speed = bulletSpeed;
        damage = bulletDamage;
        enemyMask = enemyLayer;
        owner = ownerObj;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = dir * speed;
        }

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // не бьём владельца
        if (owner != null && other.transform.root.gameObject == owner) return;

        // проверка по LayerMask
        if (((1 << other.gameObject.layer) & enemyMask.value) == 0) return;

        // ВАЖНО: health часто на root/parent
        Health h = other.GetComponentInParent<Health>();
        if (h != null)
        {
            Debug.Log("BULLET HIT: " + other.name);
            h.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
