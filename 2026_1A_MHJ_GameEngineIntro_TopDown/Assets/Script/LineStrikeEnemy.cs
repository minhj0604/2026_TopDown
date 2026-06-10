using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class LineStrikeEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver, IParryableEnemyAttack, IDodgeableEnemyAttack
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Color prepareColor = new Color(1f, 0.8f, 0.15f, 1f);
    [SerializeField] private Color attackColor = new Color(1f, 0.15f, 0.05f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackWidth = 0.32f;
    [SerializeField] private float attackActiveTime = 0.16f;
    [SerializeField] private float attackCooldown = 1.45f;
    [SerializeField] private float stopDistance = 1.45f;
    [SerializeField] private float detectRange = 2.8f;
    [SerializeField] private float parriedStunTime = 0.9f;
    [SerializeField] private float parriedKnockbackMultiplier = 3f;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float knockbackTime = 0.12f;
    [SerializeField] private float hitStunTime = 0.3f;

    public bool IsDead => currentHealth <= 0f && !isTimeStopped;
    public bool IsParryableAttackActive => isAttackActive && !IsDead && !isTimeStopped && groggyTimer <= 0f;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private EnemyHealthBar healthBar;
    private Rigidbody2D rb;
    private Transform player;
    private DungeonRunManager dungeonRunManager;
    private Coroutine attackRoutine;
    private float currentHealth;
    private float cooldownTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private float knockbackTimer;
    private float hitStunTimer;
    private Vector2 knockbackVelocity;
    private Vector2 attackDirection = Vector2.down;
    private bool isTimeStopped;
    private bool isAttackActive;
    private bool pendingTimeStopHit;
    private bool defeatHandled;
    private Vector2 pendingTimeStopKnockback;
    private Color normalColor;
    private LineRenderer attackPreview;

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
        SetupAttackPreview();
        ResetEnemy();
    }

    private void Update()
    {
        TickTimers();
        if (hitStunTimer <= 0f && CanStartAttack())
            attackRoutine = StartCoroutine(AttackRoutine());
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

        if (attackRoutine != null || hitStunTimer > 0f || groggyTimer > 0f) return;

        float distance = Vector2.Distance(rb.position, player.position);
        if (distance <= detectRange && distance > stopDistance)
        {
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            rb.MovePosition(rb.position + direction * GetMoveSpeed() * Time.fixedDeltaTime);
        }
        else
        {
            UpdateAttackPreview(false);
        }
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
            StopAttackRoutine();
            spriteRenderer.color = hitColor;
            hitFlashTimer = hitFlashTime;
            return;
        }

        StartKnockback(hitDirection);
        hitStunTimer = hitStunTime;
        StopAttackRoutine();
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
        cooldownTimer = attackCooldown;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        knockbackTimer = 0f;
        hitStunTimer = 0f;
        knockbackVelocity = Vector2.zero;
        isTimeStopped = false;
        isAttackActive = false;
        pendingTimeStopHit = false;
        defeatHandled = false;
        pendingTimeStopKnockback = Vector2.zero;
        StopAttackRoutine();
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
                StartKnockback(pendingTimeStopKnockback);
                hitStunTimer = hitStunTime;
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
        StopAttackRoutine();
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    public void OnParried(Vector2 parryDirection)
    {
        if (IsDead) return;
        StopAttackRoutine();
        cooldownTimer = attackCooldown;
        hitStunTimer = Mathf.Max(hitStunTimer, parriedStunTime);
        groggyTimer = Mathf.Max(groggyTimer, parriedStunTime);
        StartKnockback(parryDirection.normalized * parriedKnockbackMultiplier);
        spriteRenderer.color = groggyColor;
    }

    public bool IsDodgeableAttackActiveFor(Vector2 playerPosition)
    {
        return IsParryableAttackActive && IsInsideLine(playerPosition);
    }

    private IEnumerator AttackRoutine()
    {
        attackDirection = ((Vector2)player.position - rb.position).normalized;
        spriteRenderer.color = prepareColor;
        rb.linearVelocity = Vector2.zero;
        UpdateAttackPreview(true);
        yield return new WaitForSeconds(enemyData != null ? enemyData.shootStandTime : 0.35f);

        if (!CanAct() || IsDead || player == null || isTimeStopped || groggyTimer > 0f)
        {
            attackRoutine = null;
            RefreshColor();
            yield break;
        }

        isAttackActive = true;
        spriteRenderer.color = attackColor;
        UpdateAttackPreview(true);
        HitPlayerIfInsideLine();
        yield return new WaitForSeconds(attackActiveTime);

        isAttackActive = false;
        UpdateAttackPreview(false);
        cooldownTimer = attackCooldown;
        attackRoutine = null;
        RefreshColor();
    }

    private void HitPlayerIfInsideLine()
    {
        if (player == null || !IsInsideLine(player.position))
            return;
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;
        playerHealth.TakeDamage(enemyData != null ? enemyData.contactDamage : 10f, attackDirection);
    }

    private bool IsInsideLine(Vector2 point)
    {
        Vector2 fromEnemy = point - rb.position;
        float forward = Vector2.Dot(fromEnemy, attackDirection);
        if (forward < 0f || forward > attackRange)
            return false;

        Vector2 closest = rb.position + attackDirection * forward;
        return Vector2.Distance(point, closest) <= attackWidth;
    }

    private bool CanStartAttack()
    {
        return CanAct()
            && !IsDead
            && player != null
            && !isTimeStopped
            && groggyTimer <= 0f
            && cooldownTimer <= 0f
            && attackRoutine == null
            && Vector2.Distance(rb.position, player.position) <= attackRange;
    }

    private void SetupAttackPreview()
    {
        GameObject previewObject = new GameObject("Attack Preview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        attackPreview = previewObject.AddComponent<LineRenderer>();
        attackPreview.useWorldSpace = true;
        attackPreview.positionCount = 2;
        attackPreview.startWidth = attackWidth * 2f;
        attackPreview.endWidth = attackWidth * 2f;
        attackPreview.material = new Material(Shader.Find("Sprites/Default"));
        attackPreview.startColor = new Color(1f, 0.05f, 0.05f, 0.42f);
        attackPreview.endColor = new Color(1f, 0.05f, 0.05f, 0.42f);
        attackPreview.sortingOrder = 20;
        attackPreview.enabled = false;
    }

    private void UpdateAttackPreview(bool visible)
    {
        if (attackPreview == null)
            return;

        attackPreview.enabled = visible;
        if (!visible)
            return;

        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(attackDirection.normalized * attackRange);
        attackPreview.startWidth = attackWidth * 2f;
        attackPreview.endWidth = attackWidth * 2f;
        attackPreview.SetPosition(0, start);
        attackPreview.SetPosition(1, end);
    }

    private void TickTimers()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
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
                RefreshColor();
        }
        if (knockbackTimer > 0f)
            knockbackTimer -= Time.deltaTime;
        if (hitStunTimer > 0f)
            hitStunTimer -= Time.deltaTime;
    }

    private void StopAttackRoutine()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = null;
        isAttackActive = false;
        UpdateAttackPreview(false);
    }

    private void ApplyData()
    {
        normalColor = enemyData != null ? enemyData.color : new Color(0.35f, 0.75f, 1f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null ? enemyData.sprite : GetGeneratedSprite();
    }

    private float GetMaxHealth() => enemyData != null ? enemyData.maxHealth : 38f;
    private float GetMoveSpeed() => enemyData != null ? enemyData.moveSpeed * 0.75f : 0.65f;
    private float GetKnockbackForce() => enemyData != null ? enemyData.knockbackForce : 2.2f;

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
        StopAttackRoutine();
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
        generatedSprite.name = "Generated Line Strike Enemy Sprite";
        return generatedSprite;
    }
}
