using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DashConeEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver, IParryableEnemyAttack, IDodgeableEnemyAttack
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Color prepareColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color attackColor = new Color(1f, 0.15f, 0.05f, 1f);
    [SerializeField] private Color retreatColor = new Color(0.55f, 0.55f, 1f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float attackRange = 1.1f;
    [SerializeField] private float attackAngle = 95f;
    [SerializeField] private float idleDetectRange = 2.3f;
    [SerializeField] private float secondHitDelay = 0.18f;
    [SerializeField] private float retreatDistance = 0.75f;
    [SerializeField] private float retreatTime = 0.22f;
    [SerializeField] private float parriedStunTime = 0.9f;
    [SerializeField] private float parriedKnockbackMultiplier = 3.2f;
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
    private bool hitPlayerThisAttack;
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
        rb.linearDamping = 1.5f;

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

        UpdateAttackPreview(false);
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
        cooldownTimer = enemyData != null ? enemyData.chargeCooldown : 1.5f;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        knockbackTimer = 0f;
        hitStunTimer = 0f;
        knockbackVelocity = Vector2.zero;
        isTimeStopped = false;
        isAttackActive = false;
        hitPlayerThisAttack = false;
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
        cooldownTimer = enemyData != null ? enemyData.chargeCooldown : 1.5f;
        hitStunTimer = Mathf.Max(hitStunTimer, parriedStunTime);
        groggyTimer = Mathf.Max(groggyTimer, parriedStunTime);
        StartKnockback(parryDirection.normalized * parriedKnockbackMultiplier);
        spriteRenderer.color = groggyColor;
    }

    public bool IsDodgeableAttackActiveFor(Vector2 playerPosition)
    {
        return IsParryableAttackActive && IsInsideCone(playerPosition);
    }

    private IEnumerator AttackRoutine()
    {
        attackDirection = ((Vector2)player.position - rb.position).normalized;
        spriteRenderer.color = prepareColor;
        rb.linearVelocity = Vector2.zero;
        UpdateAttackPreview(true);
        yield return new WaitForSeconds(enemyData != null ? enemyData.chargePrepareTime : 0.45f);

        if (!CanAct() || IsDead || player == null || isTimeStopped || groggyTimer > 0f)
        {
            attackRoutine = null;
            UpdateAttackPreview(false);
            RefreshColor();
            yield break;
        }

        spriteRenderer.color = retreatColor;
        UpdateAttackPreview(false);
        yield return MoveForTime(-attackDirection * retreatDistance, retreatTime);

        yield return new WaitForSeconds(secondHitDelay);
        yield return DashAttackStep();

        yield return new WaitForSeconds(secondHitDelay);
        if (player != null)
            attackDirection = ((Vector2)player.position - rb.position).normalized;
        yield return DashAttackStep();

        cooldownTimer = enemyData != null ? enemyData.chargeCooldown : 1.5f;
        attackRoutine = null;
        RefreshColor();
    }

    private IEnumerator DashAttackStep()
    {
        if (!CanAct() || IsDead || player == null || isTimeStopped || groggyTimer > 0f)
            yield break;

        spriteRenderer.color = attackColor;
        isAttackActive = true;
        hitPlayerThisAttack = false;
        UpdateAttackPreview(true);

        yield return DashStep(enemyData != null ? enemyData.chargeDuration : 0.28f);

        isAttackActive = false;
        UpdateAttackPreview(false);
    }

    private IEnumerator DashStep(float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            if (isTimeStopped || groggyTimer > 0f)
                break;
            rb.MovePosition(rb.position + attackDirection * GetChargeSpeed() * Time.fixedDeltaTime);
            UpdateAttackPreview(true);
            if (!hitPlayerThisAttack && TryHitPlayerIfInsideCone())
                hitPlayerThisAttack = true;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator MoveForTime(Vector2 move, float duration)
    {
        Vector2 start = rb.position;
        Vector2 target = start + move;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(start, target, Mathf.Clamp01(timer / duration)));
            yield return null;
        }
        rb.position = target;
    }

    private bool TryHitPlayerIfInsideCone()
    {
        if (player == null || !IsInsideCone(player.position))
            return false;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return false;

        playerHealth.TakeDamage(enemyData != null ? enemyData.contactDamage : 10f, attackDirection);
        return true;
    }

    private bool IsInsideCone(Vector2 point)
    {
        Vector2 toPoint = point - rb.position;
        if (toPoint.magnitude > attackRange)
            return false;
        if (toPoint.sqrMagnitude <= 0.01f)
            return true;

        return Vector2.Angle(attackDirection, toPoint.normalized) <= attackAngle * 0.5f;
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
            && Vector2.Distance(rb.position, player.position) <= Mathf.Min(idleDetectRange, enemyData != null ? enemyData.chargeStartRange : 1.8f);
    }

    private void SetupAttackPreview()
    {
        GameObject previewObject = new GameObject("Attack Preview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        attackPreview = previewObject.AddComponent<LineRenderer>();
        attackPreview.useWorldSpace = true;
        attackPreview.loop = false;
        attackPreview.positionCount = 0;
        attackPreview.startWidth = 0.035f;
        attackPreview.endWidth = 0.035f;
        attackPreview.material = new Material(Shader.Find("Sprites/Default"));
        attackPreview.startColor = new Color(1f, 0.1f, 0.05f, 0.8f);
        attackPreview.endColor = new Color(1f, 0.1f, 0.05f, 0.8f);
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

        int arcSteps = 12;
        attackPreview.positionCount = arcSteps + 3;
        Vector3 origin = transform.position;
        attackPreview.SetPosition(0, origin);

        float startAngle = -attackAngle * 0.5f;
        for (int i = 0; i <= arcSteps; i++)
        {
            float angle = startAngle + attackAngle * i / arcSteps;
            Vector2 direction = RotateVector(attackDirection, angle);
            attackPreview.SetPosition(i + 1, origin + (Vector3)(direction * attackRange));
        }

        attackPreview.SetPosition(arcSteps + 2, origin);
    }

    private Vector2 RotateVector(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
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
        normalColor = enemyData != null ? enemyData.color : new Color(0.9f, 0.25f, 0.25f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null ? enemyData.sprite : GetGeneratedSprite();
    }

    private float GetMaxHealth() => enemyData != null ? enemyData.maxHealth : 44f;
    private float GetMoveSpeed() => enemyData != null ? enemyData.moveSpeed : 0.85f;
    private float GetChargeSpeed() => enemyData != null ? enemyData.chargeSpeed : 4.2f;
    private float GetKnockbackForce() => enemyData != null ? enemyData.knockbackForce : 2.4f;

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
        generatedSprite.name = "Generated Dash Cone Enemy Sprite";
        return generatedSprite;
    }
}
