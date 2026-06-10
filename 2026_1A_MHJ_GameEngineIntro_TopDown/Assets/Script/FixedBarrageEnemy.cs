using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FixedBarrageEnemy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Color fireColor = new Color(0.95f, 0.25f, 1f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private int patternShotCount = 8;
    [SerializeField] private float patternInterval = 1.4f;
    [SerializeField] private float patternRotateDegrees = 18f;
    [SerializeField] private float fireFlashTime = 0.08f;
    [SerializeField] private float hitFlashTime = 0.08f;

    public bool IsDead => currentHealth <= 0f && !isTimeStopped;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private EnemyHealthBar healthBar;
    private Rigidbody2D rb;
    private DungeonRunManager dungeonRunManager;
    private float currentHealth;
    private float patternTimer;
    private float hitFlashTimer;
    private float groggyTimer;
    private float patternRotation;
    private bool isTimeStopped;
    private bool pendingTimeStopHit;
    private bool defeatHandled;
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
        rb.linearDamping = 3f;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.5f, 0.5f);

        dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();
        ApplyData();
        ResetEnemy();
    }

    private void Update()
    {
        if (patternTimer > 0f)
            patternTimer -= Time.deltaTime;
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

        if (CanFire())
            FirePattern();
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
            pendingTimeStopKnockback = hitDirection;
            spriteRenderer.color = hitColor;
            hitFlashTimer = hitFlashTime;
            return;
        }

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
        patternTimer = 0.5f;
        hitFlashTimer = 0f;
        groggyTimer = 0f;
        patternRotation = 0f;
        isTimeStopped = false;
        pendingTimeStopHit = false;
        defeatHandled = false;
        pendingTimeStopKnockback = Vector2.zero;
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
        rb.linearVelocity = Vector2.zero;
        RefreshColor();
    }

    private bool CanFire()
    {
        return CanAct() && !IsDead && !isTimeStopped && groggyTimer <= 0f && patternTimer <= 0f;
    }

    private void FirePattern()
    {
        spriteRenderer.color = fireColor;
        hitFlashTimer = fireFlashTime;

        int count = Mathf.Max(1, patternShotCount);
        for (int i = 0; i < count; i++)
        {
            float angle = patternRotation + 360f * i / count;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            SpawnProjectile(direction);
        }

        patternRotation += patternRotateDegrees;
        patternTimer = patternInterval;
    }

    private void SpawnProjectile(Vector2 direction)
    {
        EnemyProjectile projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, rb.position, Quaternion.identity);
        }
        else
        {
            GameObject projectileObject = new GameObject("EnemyProjectile");
            projectileObject.transform.position = rb.position;
            projectile = projectileObject.AddComponent<EnemyProjectile>();
        }

        projectile.Launch(direction, enemyData != null ? enemyData.projectileSpeed : 2.3f, enemyData != null ? enemyData.projectileDamage : 6f);
    }

    private void ApplyData()
    {
        normalColor = enemyData != null ? enemyData.color : new Color(0.55f, 0.3f, 1f, 1f);
        spriteRenderer.color = normalColor;
        spriteRenderer.sprite = enemyData != null && enemyData.sprite != null ? enemyData.sprite : GetGeneratedSprite();
    }

    private float GetMaxHealth() => enemyData != null ? enemyData.maxHealth : 42f;

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
        generatedSprite.name = "Generated Fixed Barrage Enemy Sprite";
        return generatedSprite;
    }
}
