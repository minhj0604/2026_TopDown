using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BombThrowerEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private ExplodingEnemyProjectile projectilePrefab;
    [SerializeField] private Color prepareColor = new Color(1f, 0.65f, 0.15f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float throwInterval = 1.7f;
    [SerializeField] private float throwPrepareTime = 0.45f;
    [SerializeField] private float bombFuseTime = 1f;
    [SerializeField] private float bombExplosionRadius = 0.75f;
    [SerializeField] private float panicBackstepDistance = 1.45f;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float knockbackTime = 0.12f;
    [SerializeField] private float hitStunTime = 0.3f;
    [SerializeField] private float hitGroggyTime = 0.8f;
    [SerializeField] private float retreatAfterGroggyTime = 0.7f;
    [SerializeField] private float postRetreatThrowDelay = 0.3f;
    [SerializeField] private float separationRange = 0.8f;
    [SerializeField] private float separationWeight = 0.9f;

    public bool IsDead => currentHealth <= 0f && !isTimeStopped;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private EnemyHealthBar healthBar;
    private Rigidbody2D rb;
    private Transform player;
    private DungeonRunManager dungeonRunManager;
    private Coroutine throwRoutine;
    private float currentHealth;
    private float throwTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private float retreatTimer;
    private float knockbackTimer;
    private float hitStunTimer;
    private Vector2 knockbackVelocity;
    private bool isTimeStopped;
    private bool pendingTimeStopHit;
    private bool defeatHandled;
    private bool retreatAfterGroggy;
    private Vector2 pendingTimeStopKnockback;
    private Color normalColor;

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

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            player = playerHealth.transform;
        dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();

        ApplyData();
        ResetEnemy();
    }

    private void Update()
    {
        TickTimers();
        if (hitStunTimer <= 0f && retreatTimer <= 0f && CanStartThrow())
            throwRoutine = StartCoroutine(ThrowRoutine());
    }

    private void FixedUpdate()
    {
        if (!CanAct()) return;
        if (IsDead || player == null || isTimeStopped) return;
        if (throwRoutine != null) return;

        if (knockbackTimer > 0f)
        {
            rb.MovePosition(rb.position + knockbackVelocity * Time.fixedDeltaTime);
            return;
        }

        if (hitStunTimer > 0f || groggyTimer > 0f) return;
        if (retreatTimer > 0f)
        {
            MoveAwayFromPlayer(GetRetreatDistance());
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;
        float preferredDistance = enemyData != null ? enemyData.preferredDistance : 2.4f;
        Vector2 moveDirection = Vector2.zero;
        if (distance < Mathf.Min(preferredDistance - 0.25f, panicBackstepDistance))
            moveDirection = -toPlayer.normalized;

        if (moveDirection.sqrMagnitude > 0.01f)
            moveDirection = EnemySeparationUtility.AddSeparation(this, moveDirection, separationRange, separationWeight);

        rb.MovePosition(rb.position + moveDirection * GetMoveSpeed() * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (healthBar != null)
            healthBar.SetValue(currentHealth, GetMaxHealth());

        if (isTimeStopped)
        {
            pendingTimeStopHit = true;
            if (hitDirection.sqrMagnitude > pendingTimeStopKnockback.sqrMagnitude)
                pendingTimeStopKnockback = hitDirection;
            StopThrowRoutine();
            spriteRenderer.color = hitColor;
            hitFlashTimer = hitFlashTime;
            return;
        }

        StartHitReaction(hitDirection);
        spriteRenderer.color = hitColor;
        hitFlashTimer = hitFlashTime;

        if (IsDead)
            HandleDefeat();
    }

    public void ResetEnemy()
    {
        currentHealth = GetMaxHealth();
        if (healthBar != null)
            healthBar.SetValue(currentHealth, GetMaxHealth());
        throwTimer = throwInterval;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        retreatTimer = 0f;
        knockbackTimer = 0f;
        hitStunTimer = 0f;
        knockbackVelocity = Vector2.zero;
        isTimeStopped = false;
        pendingTimeStopHit = false;
        defeatHandled = false;
        retreatAfterGroggy = false;
        pendingTimeStopKnockback = Vector2.zero;
        StopThrowRoutine();
        rb.linearVelocity = Vector2.zero;
        ApplyData();
    }

    public void SetTimeStopped(bool isStopped)
    {
        if (IsDead) return;

        isTimeStopped = isStopped;
        rb.linearVelocity = Vector2.zero;

        if (!isTimeStopped)
        {
            if (pendingTimeStopHit)
            {
                StartHitReaction(pendingTimeStopKnockback);
                pendingTimeStopHit = false;
                pendingTimeStopKnockback = Vector2.zero;
            }

            if (currentHealth <= 0f)
            {
                HandleDefeat();
                return;
            }
        }

        RefreshColor();
    }

    public void ApplyGroggy(float duration)
    {
        if (IsDead) return;

        groggyTimer = duration;
        retreatAfterGroggy = false;
        retreatTimer = 0f;
        StopThrowRoutine();
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    private IEnumerator ThrowRoutine()
    {
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = prepareColor;
        yield return new WaitForSeconds(throwPrepareTime);

        if (CanAct() && !IsDead && player != null && !isTimeStopped && groggyTimer <= 0f)
            ThrowBomb();

        throwTimer = throwInterval;
        throwRoutine = null;
        RefreshColor();
    }

    private void ThrowBomb()
    {
        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        ExplodingEnemyProjectile projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, rb.position, Quaternion.identity);
        }
        else
        {
            GameObject projectileObject = new GameObject("ExplodingEnemyProjectile");
            projectileObject.transform.position = rb.position;
            projectile = projectileObject.AddComponent<ExplodingEnemyProjectile>();
        }

        projectile.Launch(direction, enemyData != null ? enemyData.projectileSpeed : 2.2f, enemyData != null ? enemyData.projectileDamage : 10f, bombFuseTime, bombExplosionRadius);
    }

    private bool CanStartThrow()
    {
        if (!CanAct() || IsDead || player == null || isTimeStopped || groggyTimer > 0f || retreatTimer > 0f || throwTimer > 0f || throwRoutine != null)
            return false;

        float throwRange = enemyData != null ? enemyData.shootRange : 3.2f;
        return ((Vector2)player.position - rb.position).sqrMagnitude <= throwRange * throwRange;
    }

    private void TickTimers()
    {
        if (throwTimer > 0f)
            throwTimer -= Time.deltaTime;
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
                if (retreatAfterGroggy)
                    StartRetreatAfterGroggy();
                RefreshColor();
            }
        }
        if (retreatTimer > 0f)
            retreatTimer -= Time.deltaTime;
        if (knockbackTimer > 0f)
            knockbackTimer -= Time.deltaTime;
        if (hitStunTimer > 0f)
            hitStunTimer -= Time.deltaTime;
    }

    private void StopThrowRoutine()
    {
        if (throwRoutine != null)
            StopCoroutine(throwRoutine);
        throwRoutine = null;
    }

    private void ApplyData()
    {
        normalColor = enemyData != null ? enemyData.color : new Color(0.95f, 0.55f, 0.2f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null ? enemyData.sprite : GetGeneratedSprite();
    }

    private float GetMaxHealth() => enemyData != null ? enemyData.maxHealth : 36f;
    private float GetMoveSpeed() => enemyData != null ? enemyData.moveSpeed * 0.55f : 0.45f;
    private float GetKnockbackForce() => enemyData != null ? enemyData.knockbackForce : 2f;

    private float GetRetreatDistance()
    {
        float preferredDistance = enemyData != null ? enemyData.preferredDistance : 2.4f;
        return Mathf.Max(preferredDistance, panicBackstepDistance);
    }

    private void StartHitReaction(Vector2 hitDirection)
    {
        StartKnockback(hitDirection);
        hitStunTimer = hitStunTime;
        groggyTimer = Mathf.Max(groggyTimer, hitGroggyTime);
        retreatAfterGroggy = true;
        retreatTimer = 0f;
        StopThrowRoutine();
    }

    private void StartRetreatAfterGroggy()
    {
        retreatAfterGroggy = false;
        retreatTimer = retreatAfterGroggyTime;
        throwTimer = Mathf.Max(throwTimer, retreatAfterGroggyTime + postRetreatThrowDelay);
    }

    private void MoveAwayFromPlayer(float targetDistance)
    {
        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.sqrMagnitude <= 0.01f)
            toPlayer = Vector2.down;

        float currentDistance = toPlayer.magnitude;
        if (currentDistance >= targetDistance)
            return;

        Vector2 moveDirection = EnemySeparationUtility.AddSeparation(this, -toPlayer.normalized, separationRange, separationWeight);
        rb.MovePosition(rb.position + moveDirection * GetMoveSpeed() * Time.fixedDeltaTime);
    }

    private void StartKnockback(Vector2 hitDirection)
    {
        if (hitDirection.sqrMagnitude <= 0.01f)
            return;

        knockbackTimer = knockbackTime;
        knockbackVelocity = hitDirection.normalized * GetKnockbackForce() * Mathf.Max(1f, hitDirection.magnitude);
    }

    private void RefreshColor()
    {
        if (isTimeStopped)
            spriteRenderer.color = timeStopColor;
        else if (groggyTimer > 0f)
            spriteRenderer.color = groggyColor;
        else
            spriteRenderer.color = normalColor;
    }

    private void HandleDefeat()
    {
        if (defeatHandled)
            return;

        defeatHandled = true;
        StopThrowRoutine();
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = hitColor;
        Debug.Log($"{name} defeated.", this);
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
        generatedSprite.name = "Generated Bomb Thrower Enemy Sprite";
        return generatedSprite;
    }
}
