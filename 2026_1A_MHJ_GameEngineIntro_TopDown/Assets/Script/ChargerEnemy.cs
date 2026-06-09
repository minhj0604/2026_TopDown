using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ChargerEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Color prepareColor = new Color(1f, 0.45f, 0.15f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float knockbackTime = 0.12f;
    [SerializeField] private float hitStunTime = 0.32f;
    [SerializeField] private float contactDamageCenterDistance = 0.38f;

    public bool IsDead => currentHealth <= 0f;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private EnemyHealthBar healthBar;
    private Rigidbody2D rb;
    private Transform player;
    private DungeonRunManager dungeonRunManager;
    private Coroutine chargeRoutine;
    private float currentHealth;
    private float cooldownTimer;
    private float contactTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private float knockbackTimer;
    private float hitStunTimer;
    private Vector2 knockbackVelocity;
    private bool isTimeStopped;
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
        rb.linearDamping = 1f;

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
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
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
                RefreshColor();
        }

        if (knockbackTimer > 0f)
            knockbackTimer -= Time.deltaTime;
        if (hitStunTimer > 0f)
            hitStunTimer -= Time.deltaTime;

        if (hitStunTimer <= 0f && CanStartCharge())
            chargeRoutine = StartCoroutine(ChargeRoutine());
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

        if (chargeRoutine != null) return;

        float distance = Vector2.Distance(rb.position, player.position);
        float startRange = enemyData != null ? enemyData.chargeStartRange : 1.8f;
        if (distance > startRange)
        {
            Vector2 direction = ((Vector2)player.position - rb.position).normalized;
            float moveSpeed = enemyData != null ? enemyData.moveSpeed : 0.35f;
            rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
        }
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
        playerHealth.TakeDamage(enemyData != null ? enemyData.contactDamage : 10f, hitDirection);
        contactTimer = enemyData != null ? enemyData.contactDamageCooldown : 0.7f;
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
            StopCharge();
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
        cooldownTimer = 0.5f;
        contactTimer = 0f;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        knockbackTimer = 0f;
        hitStunTimer = 0f;
        knockbackVelocity = Vector2.zero;
        isTimeStopped = false;
        StopCharge();
        rb.linearVelocity = Vector2.zero;
        ApplyData();
    }

    public void SetTimeStopped(bool isStopped)
    {
        if (IsDead) return;

        isTimeStopped = isStopped;
        if (isStopped)
            rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    public void ApplyGroggy(float duration)
    {
        if (IsDead) return;

        groggyTimer = duration;
        StopCharge();
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    private bool CanStartCharge()
    {
        return CanAct()
            && !IsDead
            && player != null
            && !isTimeStopped
            && groggyTimer <= 0f
            && cooldownTimer <= 0f
            && chargeRoutine == null
            && Vector2.Distance(rb.position, player.position) <= (enemyData != null ? enemyData.chargeStartRange : 1.8f);
    }

    private IEnumerator ChargeRoutine()
    {
        spriteRenderer.color = prepareColor;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(enemyData != null ? enemyData.chargePrepareTime : 0.45f);

        if (!CanAct() || IsDead || player == null || isTimeStopped || groggyTimer > 0f)
        {
            chargeRoutine = null;
            RefreshColor();
            yield break;
        }

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        float chargeTimer = enemyData != null ? enemyData.chargeDuration : 0.35f;
        while (chargeTimer > 0f)
        {
            if (isTimeStopped || groggyTimer > 0f)
                break;

            rb.MovePosition(rb.position + direction * GetChargeSpeed() * Time.fixedDeltaTime);
            chargeTimer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        cooldownTimer = enemyData != null ? enemyData.chargeCooldown : 1.4f;
        chargeRoutine = null;
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    private void StopCharge()
    {
        if (chargeRoutine != null)
            StopCoroutine(chargeRoutine);
        chargeRoutine = null;
    }

    private void ApplyData()
    {
        normalColor = enemyData != null ? enemyData.color : new Color(0.95f, 0.35f, 0.35f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null ? enemyData.sprite : GetGeneratedSprite();
    }

    private float GetChargeSpeed() => enemyData != null ? enemyData.chargeSpeed : 4f;
    private float GetMaxHealth() => enemyData != null ? enemyData.maxHealth : 40f;
    private float GetKnockbackForce() => enemyData != null ? enemyData.knockbackForce : 2.5f;

    private void StartKnockback(Vector2 hitDirection)
    {
        if (hitDirection.sqrMagnitude <= 0.01f)
            return;

        StopCharge();
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
        generatedSprite.name = "Generated Charger Enemy Sprite";
        return generatedSprite;
    }
}
