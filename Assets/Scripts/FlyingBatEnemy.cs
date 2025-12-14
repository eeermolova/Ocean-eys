using UnityEngine;
using System.Collections;

public class FlyingBatEnemy : MonoBehaviour
{
    [Header("Настройки патрулирования")]
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float patrolChangeTime = 3f;
    [SerializeField] private Vector2 patrolAreaMin = new Vector2(-10, 2);
    [SerializeField] private Vector2 patrolAreaMax = new Vector2(10, 5);

    [Header("Настройки преследования")]
    [SerializeField] private float chaseSpeed = 6f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 0.37f;

    [Header("Настройки атаки")]
    [SerializeField] private float attackDamage = 25f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackKnockback = 5f;

    [Header("Визуальные настройки")]
    [SerializeField] private GameObject attackIndicator;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color attackColor = Color.magenta;

    [Header("Флип (направление спрайта)")]
    [SerializeField] private bool spriteFacesRightByDefault = false;

    [Header("Анимации (Bat)")]
    [SerializeField] private bool useAnimator = true;
    [SerializeField] private float chaseAnimSpeedMultiplier = 1.2f; // опционально
    [SerializeField] private bool hasAttackAnimation = false;
    [Header("Смерть (удаление)")]
    [SerializeField] private float destroyDelay = 0.8f; // поставь примерно длину Bat_death
    [SerializeField] private bool disableColliderOnDeath = true;

    private bool deathHandled = false;
    private Collider2D myCollider;

    private Animator animator;
    private bool deathAnimSent = false;

    private static readonly int ANIM_ATTACK = Animator.StringToHash("Attack");
    private static readonly int ANIM_DEAD = Animator.StringToHash("IsDead");

    // Компоненты
    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Health enemyHealth;

    // Состояние
    private enum State { Patrolling, Chasing, Attacking, Cooldown }
    private State currentState = State.Patrolling;

    // Патрулирование
    private Vector2 patrolTarget;
    private float nextDirectionChangeTime;

    // Атака
    private bool canAttack = true;
    private float lastAttackTime;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        enemyHealth = GetComponent<Health>();
        if (enemyHealth == null) enemyHealth = GetComponentInChildren<Health>();

        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (rb == null) Debug.LogError($"{name}: Rigidbody2D не найден");
        if (spriteRenderer == null) Debug.LogError($"{name}: SpriteRenderer не найден");
    }

    private void Start()
    {
        // Настройка физики
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // Установка первой цели патрулирования
        SetRandomPatrolTarget();
        nextDirectionChangeTime = Time.time + patrolChangeTime;

        // Поиск игрока
        FindPlayer();

        // Настройка индикатора атаки
        if (attackIndicator != null)
        {
            attackIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        // Проверка здоровья
        if (enemyHealth != null && !enemyHealth.IsAlive)
        {
            HandleDeath();
            return;
        }

        // Постоянный поиск игрока
        FindPlayer();

        // Обновление состояния
        UpdateEnemyState();

        // Обработка состояний
        HandleState();

        // Визуальная обратная связь
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        // Физическое движение
        HandleMovement();
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        else
        {
            // Проверяем, жив ли игрок
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsAlive)
            {
                player = null;
                currentState = State.Patrolling;
            }
        }
    }

    private void UpdateEnemyState()
    {
        if (player == null)
        {
            currentState = State.Patrolling;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Patrolling:
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = State.Chasing;
                }
                break;

            case State.Chasing:
                if (distanceToPlayer > detectionRange * 1.2f)
                {
                    currentState = State.Patrolling;
                }
                else if (distanceToPlayer <= attackRange && canAttack)
                {
                    currentState = State.Attacking;
                }
                break;

            case State.Attacking:
                // После атаки переходим в кулдаун
                break;

            case State.Cooldown:
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    canAttack = true;
                    currentState = distanceToPlayer <= detectionRange ? State.Chasing : State.Patrolling;
                }
                break;
        }
    }

    private void HandleState()
    {
        switch (currentState)
        {
            case State.Patrolling:
                // Периодически меняем направление патрулирования
                if (Time.time >= nextDirectionChangeTime)
                {
                    SetRandomPatrolTarget();
                    nextDirectionChangeTime = Time.time + patrolChangeTime;
                }
                break;

            case State.Chasing:
                // При преследовании цель - позиция игрока
                if (player != null)
                {
                    patrolTarget = player.position;
                }
                break;

            case State.Attacking:
                AttackPlayer();
                break;
        }
    }

    private void HandleMovement()
    {
        Vector2 movement = Vector2.zero;
        float currentSpeed = patrolSpeed;

        switch (currentState)
        {
            case State.Patrolling:
                movement = (patrolTarget - (Vector2)transform.position).normalized;
                currentSpeed = patrolSpeed;
                break;

            case State.Chasing:
                if (player != null)
                {
                    movement = (player.position - transform.position).normalized;
                    currentSpeed = chaseSpeed;
                }
                break;

            case State.Attacking:
                if (player != null)
                {
                    // При атаке летим прямо на игрока с максимальной скоростью
                    movement = (player.position - transform.position).normalized;
                    currentSpeed = chaseSpeed * 1.5f;
                }
                break;

            case State.Cooldown:
                // Во время кулдаона отлетаем от игрока
                if (player != null)
                {
                    movement = (transform.position - player.position).normalized;
                    currentSpeed = patrolSpeed;
                }
                break;
        }

        // Применяем движение
        rb.linearVelocity = movement * currentSpeed;

        // Ограничиваем позицию в пределах игровой области (опционально)
        Vector2 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, patrolAreaMin.x, patrolAreaMax.x);
        currentPos.y = Mathf.Clamp(currentPos.y, patrolAreaMin.y, patrolAreaMax.y);
        transform.position = currentPos;
    }

    private void SetRandomPatrolTarget()
    {
        patrolTarget = new Vector2(
            Random.Range(patrolAreaMin.x, patrolAreaMax.x),
            Random.Range(patrolAreaMin.y, patrolAreaMax.y)
        );
    }

    private void AttackPlayer()
    {
        if (!canAttack || player == null) return;

        // Проверяем, достаточно ли близко для атаки
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance <= attackRange)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        if (!canAttack) return;

        if (useAnimator && hasAttackAnimation && animator != null)
            animator.SetTrigger(ANIM_ATTACK);

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
        {
            // Наносим урон
            playerHealth.TakeDamage(attackDamage);
            Debug.Log($"{gameObject.name} атаковал игрока! Урон: {attackDamage}");

            // Применяем отбрасывание
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDirection = (player.position - transform.position).normalized;
                playerRb.AddForce(knockbackDirection * attackKnockback, ForceMode2D.Impulse);
            }

            // Визуальный эффект атаки
            if (attackIndicator != null)
            {
                attackIndicator.SetActive(true);
                Invoke(nameof(HideAttackIndicator), 0.2f);
            }

            // Включаем кулдаун
            canAttack = false;
            lastAttackTime = Time.time;
            currentState = State.Cooldown;
        }
    }

    private void HideAttackIndicator()
    {
        if (attackIndicator != null)
        {
            attackIndicator.SetActive(false);
        }
    }

    private void HandleDeath()
    {
        if (deathHandled) return;
        deathHandled = true;

        // включаем анимацию смерти
        if (useAnimator && animator != null && !deathAnimSent)
        {
            deathAnimSent = true;
            animator.SetBool(ANIM_DEAD, true);
        }

        // стоп движение
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // выключаем коллайдер, чтобы больше ничего не триггерилось
        if (disableColliderOnDeath && myCollider != null)
            myCollider.enabled = false;

        // можно отключить сам скрипт, чтобы гарантированно не работал
        enabled = false;

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        Color targetColor = normalColor;

        switch (currentState)
        {
            case State.Patrolling:
                targetColor = normalColor;
                break;
            case State.Chasing:
                targetColor = chaseColor;
                break;
            case State.Attacking:
                targetColor = attackColor;
                break;
            case State.Cooldown:
                targetColor = Color.gray;
                break;
        }

        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 5f);

        // Поворачиваем спрайт по направлению движения
        if (rb != null && rb.linearVelocity.x > 0.1f)
        {
            // движемся вправо
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        }
        else if (rb.linearVelocity.x < -0.1f)
        {
            // движемся влево
            spriteRenderer.flipX = spriteFacesRightByDefault;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Если столкнулись с игроком - атакуем
        if (collision.CompareTag("Player") && canAttack && currentState != State.Cooldown)
        {
            PerformAttack();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // При столкновении с игроком - атакуем
        if (collision.gameObject.CompareTag("Player") && canAttack && currentState != State.Cooldown)
        {
            PerformAttack();
        }

        // При столкновении со стенами меняем направление патрулирования
        if (currentState == State.Patrolling)
        {
            SetRandomPatrolTarget();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Зона патрулирования
        Gizmos.color = Color.blue;
        Vector2 center = new Vector2(
            (patrolAreaMin.x + patrolAreaMax.x) / 2,
            (patrolAreaMin.y + patrolAreaMax.y) / 2
        );
        Vector2 size = new Vector2(
            patrolAreaMax.x - patrolAreaMin.x,
            patrolAreaMax.y - patrolAreaMin.y
        );
        Gizmos.DrawWireCube(center, size);

        // Текущая цель патрулирования
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(patrolTarget, 0.3f);
        Gizmos.DrawLine(transform.position, patrolTarget);

        // Зона обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Зона атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Направление движения
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, rb != null ? rb.linearVelocity.normalized : Vector2.right);
    }
}