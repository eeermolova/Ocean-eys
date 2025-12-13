using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCombat : MonoBehaviour
{
    [Header("Ѕлижн€€ атака")]
    [SerializeField] private float meleeCooldown = 0.5f;
    [SerializeField] private float meleeDamage = 20f;
    [SerializeField] private float meleeRange = 1f;
    [SerializeField] public Transform attackPoint;
    [SerializeField] private LayerMask enemyLayer;

    [Header("ƒальн€€ атака (пистолет)")]
    [SerializeField] private float shootCooldown = 0.25f;
    [SerializeField] private float shootDamage = 12f;
    [SerializeField] private float bulletSpeed = 18f;
    [SerializeField] private Transform shootPoint;           // точка вылета пули
    [SerializeField] private GameObject bulletPrefab;        // префаб пули
    [SerializeField] private Camera mainCamera;              // можно не назначать (возьмЄм Camera.main)

    [SerializeField] private float gunDrawTime = 0.12f;   // подгони под длину анимации доставани€
    [SerializeField] private float gunFireDelay = 0.03f;  // на каком кадре "вылетает пул€"

    private InputSystem_Actions actions;

    private float lastMeleeTime = -999f;
    private float lastShootTime = -999f;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInParent<Animator>();

        actions = new InputSystem_Actions();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        actions.Player.Enable();
        actions.Player.Attack.performed += OnMelee;
        actions.Player.Shoot.performed += OnShoot; // <-- нужен экшен Shoot в Input Actions
    }

    private void OnDisable()
    {
        actions.Player.Attack.performed -= OnMelee;
        actions.Player.Shoot.performed -= OnShoot;
        actions.Player.Disable();
    }

    private void OnMelee(InputAction.CallbackContext context)
    {
        DoMeleeAttack();
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        Debug.Log("ѕ ћ нажата -> Shoot action сработал");

        // если кулдаун не прошЄл Ч играем "убрать"
        if (Time.time - lastShootTime < shootCooldown)
        {
            if (animator != null) animator.SetTrigger("GunHolster");
            return;
        }

        // иначе Ч запускаем последовательность достать->выстрелить
        StartCoroutine(GunShootSequence());
        
    }

    private IEnumerator GunShootSequence()
    {
        lastShootTime = Time.time;

        if (animator != null) animator.SetTrigger("GunDraw");
        yield return new WaitForSeconds(gunDrawTime);

        if (animator != null) animator.SetTrigger("GunShoot");
        yield return new WaitForSeconds(gunFireDelay);

        // “”“ вызывай твой текущий код спавна пули (он у теб€ уже есть)
        SpawnBullet();
    }

    private void DoMeleeAttack()
    {
        if (attackPoint == null) return;
        if (Time.time - lastMeleeTime < meleeCooldown) return;

        lastMeleeTime = Time.time;

        if (animator != null) animator.SetTrigger("Melee");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            meleeRange,
            enemyLayer
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
                enemyHealth.TakeDamage(meleeDamage);
        }
    }

    private void SpawnBullet()
    {
        Debug.Log("ѕ ћ: выстрел вызван");

        if (bulletPrefab == null) { Debug.LogWarning("bulletPrefab = null"); return; }
        if (shootPoint == null) { Debug.LogWarning("shootPoint = null"); return; }
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) { Debug.LogWarning("mainCamera = null"); return; }
        if (Mouse.current == null) { Debug.LogWarning("Mouse.current = null"); return; }

        if (bulletPrefab == null || shootPoint == null) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        if (Time.time - lastShootTime < shootCooldown) return;
        lastShootTime = Time.time;

        // куда целимс€ (в точку курсора)
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld3 = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
        Vector2 dir = ((Vector2)mouseWorld3 - (Vector2)shootPoint.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject b = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);

        // если на пуле есть PlayerBullet Ч инициализируем
        PlayerBullet pb = b.GetComponent<PlayerBullet>();
        if (pb != null)
        {
            pb.Init(dir, bulletSpeed, shootDamage, enemyLayer, gameObject);
        }
        else
        {
            // fallback: просто зададим скорость Rigidbody2D
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = dir * bulletSpeed;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, meleeRange);
    }
}

