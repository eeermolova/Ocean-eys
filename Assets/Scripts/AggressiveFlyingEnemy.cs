using UnityEngine;

public class AggressiveFlyingEnemy2 : MonoBehaviour
{
    [Header("Патрулирование")]
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float patrolChangeTime = 3f;
    [SerializeField] private Vector2 patrolAreaMin = new Vector2(-10, 2);
    [SerializeField] private Vector2 patrolAreaMax = new Vector2(10, 6);

    [Header("Поиск / преследование")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float detectionRange = 10f;

    [Header("Атака 1: Рывок сквозь игрока (Dash)")]
    [SerializeField] private float dashStartRange = 2.2f;
    [SerializeField] private float dashWindupTime = 0.15f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashMaxTime = 0.5f;
    [SerializeField] private float dashDamage = 25f;
    [SerializeField] private float dashPlayerKnockback = 5f;
    [SerializeField] private float dashPostHitTime = 0.08f; // чуть пролететь после попадания
    [SerializeField] private float dashRecoilSpeed = 7f;    // отлет врага назад
    [SerializeField] private float dashRecoilTime = 0.35f;
    [SerializeField] private float dashCooldown = 1.6f;

    [Header("Атака 2: Дальняя (Projectile)")]
    [SerializeField] private float rangedMinRange = 3.5f;
    [SerializeField] private float rangedMaxRange = 8f;
    [SerializeField] private float rangedWindupTime = 0.25f;
    [SerializeField] private float rangedCooldown = 1.2f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint; // можно null
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileDamage = 12f;

    [Header("Условие для стрельбы")]
    [SerializeField] private int closeAttacksBeforeRanged = 2; // после 2 ближних атак можно стрелять
    [SerializeField] private bool countCloseAttacksOnlyOnHit = true;
    // true = считаем только когда реально нанёс урон рывком
    // false = считаем каждую попытку рывка

    [Header("Визуал (необязательно)")]
    [SerializeField] private GameObject attackIndicator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color attackColor = Color.magenta;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D col;
    private Health enemyHealth;

    private enum State
    {
        Patrolling,
        Chasing,

        DashWindup,
        Dashing,
        DashRecoil,

        RangedWindup,

        Cooldown
    }

    private State currentState = State.Patrolling;

    // Патруль
    private Vector2 patrolTarget;
    private float nextDirectionChangeTime;

    // Кулдаун
    private bool canAttack = true;
    private float cooldownTimer = 0f;

    // Таймеры состояния
    private float stateTimer = 0f;

    // Dash runtime
    private Vector2 dashDir;
    private bool dashDidHit = false;
    private float dashAfterHitTimer = 0f;

    // Сколько ближних атак сделано
    private int closeAttacksDone = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        enemyHealth = GetComponent<Health>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        SetRandomPatrolTarget();
        nextDirectionChangeTime = Time.time + patrolChangeTime;

        FindPlayer();

        if (attackIndicator != null)
            attackIndicator.SetActive(false);
    }

    private void Update()
    {
        if (enemyHealth != null && !enemyHealth.IsAlive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        FindPlayer();
        TickStateTimers();
        DecideState();
        HandleNonPhysicsLogic();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (enemyHealth != null && !enemyHealth.IsAlive) return;

        HandleMovement();
        ClampToPatrolArea();
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        Health ph = player.GetComponent<Health>();
        if (ph != null && !ph.IsAlive)
        {
            player = null;
            EnterPatrol();
        }
    }

    private void DecideState()
    {
        // Не перебиваем атакующие состояния
        if (currentState == State.DashWindup ||
            currentState == State.Dashing ||
            currentState == State.DashRecoil ||
            currentState == State.RangedWindup ||
            currentState == State.Cooldown)
            return;

        if (player == null)
        {
            EnterPatrol();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > detectionRange)
        {
            EnterPatrol();
            return;
        }

        // игрок найден
        if (!canAttack)
        {
            EnterChase();
            return;
        }

        // 1) Вблизи — рывок
        if (dist <= dashStartRange)
        {
            EnterDashWindup();
            return;
        }

        // 2) Дальняя атака — только если разблокирована (после 2 ближних)
        bool rangedUnlocked = closeAttacksDone >= closeAttacksBeforeRanged;
        if (rangedUnlocked && projectilePrefab != null && dist >= rangedMinRange && dist <= rangedMaxRange)
        {
            EnterRangedWindup();
            return;
        }

        // иначе просто преследуем
        EnterChase();
    }

    private void HandleNonPhysicsLogic()
    {
        if (currentState == State.Patrolling)
        {
            if (Time.time >= nextDirectionChangeTime)
            {
                SetRandomPatrolTarget();
                nextDirectionChangeTime = Time.time + patrolChangeTime;
            }
        }
        else if (currentState == State.Chasing && player != null)
        {
            patrolTarget = player.position;
        }
    }

    private void TickStateTimers()
    {
        float dt = Time.deltaTime;

        switch (currentState)
        {
            case State.DashWindup:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                    EnterDashing();
                break;

            case State.Dashing:
                stateTimer -= dt;

                if (dashDidHit)
                {
                    dashAfterHitTimer -= dt;
                    if (dashAfterHitTimer <= 0f)
                        EnterDashRecoil();
                }
                else
                {
                    // промах/не встретили игрока — по таймеру заканчиваем
                    if (stateTimer <= 0f)
                        EnterDashRecoil();
                }
                break;

            case State.DashRecoil:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                    EnterCooldown(dashCooldown);
                break;

            case State.RangedWindup:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                {
                    FireProjectile();               // код стрельбы оставлен тем же
                    closeAttacksDone = 0;           // после выстрела сброс
                    EnterCooldown(rangedCooldown);
                }
                break;

            case State.Cooldown:
                cooldownTimer -= dt;
                if (cooldownTimer <= 0f)
                {
                    canAttack = true;

                    if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRange)
                        EnterChase();
                    else
                        EnterPatrol();
                }
                break;
        }
    }

    private void HandleMovement()
    {
        if (rb == null) return;

        Vector2 dir = Vector2.zero;
        float speed = 0f;

        switch (currentState)
        {
            case State.Patrolling:
                dir = (patrolTarget - (Vector2)transform.position).normalized;
                speed = patrolSpeed;
                break;

            case State.Chasing:
                if (player != null)
                {
                    dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    speed = chaseSpeed;
                }
                break;

            case State.DashWindup:
            case State.RangedWindup:
                dir = Vector2.zero;
                speed = 0f;
                break;

            case State.Dashing:
                dir = dashDir;
                speed = dashSpeed;
                break;

            case State.DashRecoil:
                dir = -dashDir;
                speed = dashRecoilSpeed;
                break;

            case State.Cooldown:
                // чуть отлетаем от игрока, чтобы не липнуть
                if (player != null)
                {
                    dir = ((Vector2)transform.position - (Vector2)player.position).normalized;
                    speed = patrolSpeed;
                }
                break;
        }

        rb.linearVelocity = dir * speed;
    }

    private void ClampToPatrolArea()
    {
        Vector2 p = transform.position;
        p.x = Mathf.Clamp(p.x, patrolAreaMin.x, patrolAreaMax.x);
        p.y = Mathf.Clamp(p.y, patrolAreaMin.y, patrolAreaMax.y);
        transform.position = p;
    }

    private void SetRandomPatrolTarget()
    {
        patrolTarget = new Vector2(
            Random.Range(patrolAreaMin.x, patrolAreaMax.x),
            Random.Range(patrolAreaMin.y, patrolAreaMax.y)
        );
    }

    // --------- Входы в состояния ---------

    private void EnterPatrol() => currentState = State.Patrolling;
    private void EnterChase() => currentState = State.Chasing;

    private void EnterDashWindup()
    {
        if (!canAttack || player == null) return;

        currentState = State.DashWindup;
        stateTimer = dashWindupTime;

        rb.linearVelocity = Vector2.zero;

        dashDidHit = false;
        dashAfterHitTimer = dashPostHitTime;

        dashDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        if (dashDir.sqrMagnitude < 0.0001f) dashDir = Vector2.right;

        if (attackIndicator != null) attackIndicator.SetActive(true);
    }

    private void EnterDashing()
    {
        currentState = State.Dashing;
        stateTimer = dashMaxTime;

        // Если нужно считать каждую попытку рывка как "атаку"
        if (!countCloseAttacksOnlyOnHit)
            closeAttacksDone++;

        // делаем триггер-коллайдер, чтобы пролетать сквозь игрока
        if (col != null) col.isTrigger = true;
    }

    private void EnterDashRecoil()
    {
        currentState = State.DashRecoil;
        stateTimer = dashRecoilTime;

        // возвращаем обычные коллизии
        if (col != null) col.isTrigger = false;

        if (attackIndicator != null) attackIndicator.SetActive(false);
    }

    private void EnterRangedWindup()
    {
        if (!canAttack || player == null || projectilePrefab == null) return;

        currentState = State.RangedWindup;
        stateTimer = rangedWindupTime;

        rb.linearVelocity = Vector2.zero;

        if (attackIndicator != null) attackIndicator.SetActive(true);
    }

    private void EnterCooldown(float cd)
    {
        currentState = State.Cooldown;
        canAttack = false;
        cooldownTimer = cd;

        if (attackIndicator != null) attackIndicator.SetActive(false);
    }

    // --------- Урон рывком (проходя через игрока) ---------

    private void TryDealDashDamage(Collider2D other)
    {
        if (currentState != State.Dashing) return;
        if (dashDidHit) return;

        // Важно: берём корень, чтобы работало даже если коллайдер на child
        Transform root = other.transform.root;
        if (!root.CompareTag("Player")) return;

        Health playerHealth = root.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.TakeDamage(dashDamage);

        Rigidbody2D playerRb = root.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.AddForce(dashDir * dashPlayerKnockback, ForceMode2D.Impulse);

        dashDidHit = true;
        dashAfterHitTimer = dashPostHitTime;

        if (countCloseAttacksOnlyOnHit)
            closeAttacksDone++;
    }

    // --------- Стрельба (код оставлен таким же) ---------

    private void FireProjectile()
    {
        if (player == null || projectilePrefab == null) return;

        Vector3 spawnPos = (projectileSpawnPoint != null) ? projectileSpawnPoint.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        EnemyProjectile ep = go.GetComponent<EnemyProjectile>();
        if (ep != null)
        {
            ep.Init(dir, projectileSpeed, projectileDamage, gameObject);
        }
        else
        {
            Rigidbody2D prb = go.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = dir * projectileSpeed;
        }
    }

    // --------- Коллизии ---------

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDealDashDamage(collision);
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDealDashDamage(collision);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealDashDamage(collision.collider);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDashDamage(collision.collider);

        if (currentState == State.Patrolling)
            SetRandomPatrolTarget();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        Color target = normalColor;

        switch (currentState)
        {
            case State.Patrolling:
                target = normalColor; break;
            case State.Chasing:
                target = chaseColor; break;
            case State.DashWindup:
            case State.Dashing:
            case State.DashRecoil:
            case State.RangedWindup:
                target = attackColor; break;
            case State.Cooldown:
                target = Color.gray; break;
        }

        spriteRenderer.color = Color.Lerp(spriteRenderer.color, target, Time.deltaTime * 5f);

        if (rb != null)
        {
            if (rb.linearVelocity.x > 0.1f) spriteRenderer.flipX = false;
            else if (rb.linearVelocity.x < -0.1f) spriteRenderer.flipX = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashStartRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, rangedMaxRange);

        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, rangedMinRange);

        Gizmos.color = Color.cyan;
        Vector2 center = new Vector2((patrolAreaMin.x + patrolAreaMax.x) * 0.5f, (patrolAreaMin.y + patrolAreaMax.y) * 0.5f);
        Vector2 size = new Vector2(Mathf.Abs(patrolAreaMax.x - patrolAreaMin.x), Mathf.Abs(patrolAreaMax.y - patrolAreaMin.y));
        Gizmos.DrawWireCube(center, size);
    }
}
