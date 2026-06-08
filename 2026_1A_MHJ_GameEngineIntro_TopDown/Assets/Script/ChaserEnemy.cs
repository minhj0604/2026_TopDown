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

    public bool IsDead => currentHealth <= 0f;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private DungeonRunManager dungeonRunManager;
    private float currentHealth;
    private float contactTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private bool isTimeStopped;
    private Color normalColor = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearDamping = 2f;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

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
    }

    private void FixedUpdate()
    {
        if (!CanAct()) return;
        if (IsDead || player == null || isTimeStopped || groggyTimer > 0f) return;

        Vector2 currentPosition = rb.position;
        Vector2 targetPosition = player.position;
        Vector2 direction = (targetPosition - currentPosition).normalized;
        rb.MovePosition(currentPosition + direction * GetMoveSpeed() * Time.fixedDeltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!CanAct()) return;
        if (IsDead || contactTimer > 0f) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(GetContactDamage());
        contactTimer = GetContactCooldown();
    }

    public void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (hitDirection.sqrMagnitude > 0.01f)
            rb.AddForce(hitDirection.normalized * GetKnockbackForce(), ForceMode2D.Impulse);

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
        contactTimer = 0f;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        isTimeStopped = false;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
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

        Texture2D texture = new Texture2D(16, 16);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        generatedSprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        generatedSprite.name = "Generated Chaser Enemy Sprite";
        return generatedSprite;
    }
}
