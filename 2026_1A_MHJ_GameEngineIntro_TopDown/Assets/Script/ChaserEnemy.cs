using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChaserEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float knockbackTime = 0.12f;
    [SerializeField] private float hitStunTime = 0.28f;
    [SerializeField] private float contactDamageCenterDistance = 0.38f;

    [Header("Chase")]
    [SerializeField] private float detectRange = 2.2f;
    [SerializeField] private float loseRange = 3.2f;
    [SerializeField] private float separationRange = 0.85f;
    [SerializeField] private float separationPower = 1.35f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 1.4f;
    [SerializeField] private float wanderInterval = 1.8f;
    [SerializeField] private float wanderSpeedMultiplier = 0.45f;
    [SerializeField] private float wanderArriveDistance = 0.12f;

    public bool IsDead => currentHealth <= 0f;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private EnemyHealthBar healthBar;
    private Rigidbody2D rb;
    private Transform player;
    private DungeonRunManager dungeonRunManager;
    private float currentHealth;
    private float contactTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private float knockbackTimer;
    private float hitStunTimer;
    private Vector2 knockbackVelocity;
    private Vector2 spawnPosition;
    private Vector2 wanderTarget;
    private float wanderTimer;
    private bool isChasing;
    private bool isTimeStopped;
    private Color normalColor = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (healthBar == null)
            healthBar = new GameObject("HealthBar").AddComponent<EnemyHealthBar>();
        healthBar.transform.SetParent(transform);
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 2f;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.45f, 0.45f);

        ClampChaseSettings();

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            player = playerHealth.transform;
        dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();

        ApplyData();
        ResetEnemy();
    }

    private void Update()
    {
        if (contactTimer > 0f)
            contactTimer -= Time.deltaTime;

        if (hitFlashTimer > 0f)
        {
            hitFlashTimer -= Time.deltaTime;
            if (hitFlashTimer <= 0f && !IsDead)
                RefreshColor();
        }

        if (groggyTimer > 0f)
        {
            groggyTimer -= Time.deltaTime;
            if (groggyTimer <= 0f && !IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                RefreshColor();
            }
        }

        if (knockbackTimer > 0f)
            knockbackTimer -= Time.deltaTime;
        if (hitStunTimer > 0f)
            hitStunTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (!CanAct()) return;
        if (IsDead || player == null || isTimeStopped) return;

        if (knockbackTimer > 0f)
        {
            rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            return;
        }

        if (hitStunTimer > 0f) return;
        if (groggyTimer > 0f) return;

        Vector2 currentPosition = rb.position;
        Vector2 playerPosition = player.position;
        float playerDistance = Vector2.Distance(currentPosition, playerPosition);

        if (!isChasing && playerDistance <= detectRange)
            isChasing = true;
        else if (isChasing && playerDistance >= loseRange)
            isChasing = false;

        if (isChasing)
            MoveToward(playerPosition, GetMoveSpeed(), true);
        else
            Wander();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!CanAct()) return;
        if (IsDead || contactTimer > 0f) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat != null && playerCombat.ShouldIgnoreContactDamageFrom(this))
            return;

        if (Vector2.Distance(rb.position, playerHealth.transform.position) > contactDamageCenterDistance)
            return;

        Vector2 hitDirection = ((Vector2)playerHealth.transform.position - rb.position).normalized;
        playerHealth.TakeDamage(GetContactDamage(), hitDirection);
        contactTimer = GetContactCooldown();
    }

    public void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (healthBar != null)
            healthBar.SetValue(currentHealth, GetMaxHealth());

        StartKnockback(hitDirection);
        hitStunTimer = hitStunTime;

        spriteRenderer.color = hitColor;
        hitFlashTimer = hitFlashTime;

        if (IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            spriteRenderer.color = hitColor;
            Debug.Log($"{name} defeated.", this);
        }
    }

    public void ResetEnemy()
    {
        currentHealth = GetMaxHealth();
        if (healthBar != null)
            healthBar.SetValue(currentHealth, GetMaxHealth());
        contactTimer = 0f;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        knockbackTimer = 0f;
        hitStunTimer = 0f;
        knockbackVelocity = Vector2.zero;
        spawnPosition = rb.position;
        wanderTimer = 0f;
        isChasing = false;
        isTimeStopped = false;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        PickNewWanderTarget();
        ApplyData();
    }

    public void SetTimeStopped(bool isStopped)
    {
        if (IsDead) return;

        isTimeStopped = isStopped;
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    public void ApplyGroggy(float duration)
    {
        if (IsDead) return;

        groggyTimer = duration;
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    private void ApplyData()
    {
        normalColor = enemyData != null ? enemyData.color : new Color(0.4f, 0.9f, 0.45f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null
            ? enemyData.sprite
            : GetGeneratedSprite();
    }

    private float GetMaxHealth()
    {
        return enemyData != null ? enemyData.maxHealth : 30f;
    }

    private float GetMoveSpeed()
    {
        return enemyData != null ? enemyData.moveSpeed : 1f;
    }

    private float GetContactDamage()
    {
        return enemyData != null ? enemyData.contactDamage : 8f;
    }

    private float GetContactCooldown()
    {
        return enemyData != null ? enemyData.contactDamageCooldown : 0.7f;
    }

    private float GetKnockbackForce()
    {
        return enemyData != null ? enemyData.knockbackForce : 2f;
    }

    private void StartKnockback(Vector2 hitDirection)
    {
        if (hitDirection.sqrMagnitude <= 0.01f)
            return;

        knockbackTimer = knockbackTime;
        knockbackVelocity = hitDirection.normalized * GetKnockbackForce() * Mathf.Max(1f, hitDirection.magnitude);
    }

    private void Wander()
    {
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(rb.position, wanderTarget) <= wanderArriveDistance)
            PickNewWanderTarget();

        MoveToward(wanderTarget, GetMoveSpeed() * wanderSpeedMultiplier, false);
    }

    private void PickNewWanderTarget()
    {
        wanderTimer = wanderInterval + UnityEngine.Random.Range(-0.35f, 0.35f);
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * wanderRadius;
        wanderTarget = spawnPosition + randomOffset;
    }

    private void MoveToward(Vector2 targetPosition, float moveSpeed, bool useSeparation)
    {
        Vector2 direction = targetPosition - rb.position;
        if (direction.sqrMagnitude <= 0.0025f)
            return;

        direction.Normalize();
        if (useSeparation)
        {
            Vector2 separation = GetSeparationDirection();
            direction = (direction + separation * separationPower).normalized;
        }

        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }

    private Vector2 GetSeparationDirection()
    {
        Vector2 separation = Vector2.zero;
        ChaserEnemy[] chasers = FindObjectsByType<ChaserEnemy>(FindObjectsSortMode.None);

        foreach (ChaserEnemy other in chasers)
        {
            if (other == null || other == this || other.IsDead) continue;

            Vector2 away = rb.position - other.rb.position;
            float distance = away.magnitude;
            if (distance > separationRange) continue;
            if (distance <= 0.001f)
            {
                float angle = (GetInstanceID() % 360) * Mathf.Deg2Rad;
                away = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                distance = 0.001f;
            }

            separation += away.normalized * (1f - distance / separationRange);
        }

        return separation;
    }

    private void ClampChaseSettings()
    {
        detectRange = Mathf.Clamp(detectRange, 0.8f, 2.2f);
        loseRange = Mathf.Clamp(loseRange, detectRange + 0.4f, 3.2f);
        separationRange = Mathf.Clamp(separationRange, 0.5f, 1.0f);
        separationPower = Mathf.Clamp(separationPower, 0.8f, 1.8f);
    }

    private void RefreshColor()
    {
        if (isTimeStopped)
        {
            spriteRenderer.color = timeStopColor;
            return;
        }

        if (groggyTimer > 0f)
        {
            spriteRenderer.color = groggyColor;
            return;
        }

        spriteRenderer.color = normalColor;
    }

    private bool CanAct()
    {
        if (dungeonRunManager == null)
            return true;

        return !dungeonRunManager.IsWaitingForChoice && dungeonRunManager.IsCurrentNodeCombat;
    }

    private static Sprite GetGeneratedSprite()
    {
        if (generatedSprite != null)
            return generatedSprite;

        Texture2D texture = new Texture2D(48, 48);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[48 * 48];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        generatedSprite = Sprite.Create(texture, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 100f);
        generatedSprite.name = "Generated Chaser Enemy Sprite";
        return generatedSprite;
    }
}
