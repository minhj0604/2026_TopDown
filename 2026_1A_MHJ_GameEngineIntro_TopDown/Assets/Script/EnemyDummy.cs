using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour, IDamageable, IRoomEnemy, IEnemyStatusReceiver
{
    [Header("체력")]
    [SerializeField] private float maxHealth = 50f;

    [Header("피격 반응")]
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color timeStopColor = new Color(0.25f, 0.8f, 1f, 1f);
    [SerializeField] private Color groggyColor = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("공격 타이밍 테스트")]
    [SerializeField] private float attackInterval = 2.2f;
    [SerializeField] private float attackActiveTime = 0.45f;
    [SerializeField] private float attackRange = 0.75f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private Color attackColor = new Color(1f, 0.55f, 0.1f, 1f);

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;
    public bool IsAttackActive => isAttackActive;
    public float AttackRange => attackRange;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private float currentHealth;
    private Coroutine flashRoutine;
    private Coroutine groggyRoutine;
    private Coroutine attackRoutine;
    private bool isAttackActive;
    private bool isTimeStopped;
    private bool isGroggy;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();

        spriteRenderer.color = normalColor;
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
            attackRoutine = StartCoroutine(AttackTimingRoutine());
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = null;
        isAttackActive = false;
    }

    private void Reset()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = Vector2.one;
    }

    public void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (hitDirection.sqrMagnitude > 0.01f)
            rb.AddForce(hitDirection.normalized * knockbackForce * Mathf.Max(1f, hitDirection.magnitude), ForceMode2D.Impulse);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        if (IsDead)
        {
            spriteRenderer.color = hitColor;
            Debug.Log($"{name} defeated.", this);
        }
        else
        {
            Debug.Log($"{name} hit: -{damage} HP ({currentHealth}/{maxHealth})", this);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        spriteRenderer.color = normalColor;
        rb.simulated = true;
        isAttackActive = false;
        isTimeStopped = false;
        isGroggy = false;
        if (groggyRoutine != null)
            StopCoroutine(groggyRoutine);
        groggyRoutine = null;
    }

    public void ResetEnemy()
    {
        ResetHealth();
    }

    public void SetTimeStopped(bool isStopped)
    {
        if (IsDead) return;

        isTimeStopped = isStopped;
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = isStopped ? timeStopColor : normalColor;
        if (isStopped)
            isAttackActive = false;
    }

    public void ApplyGroggy(float duration)
    {
        if (IsDead) return;

        if (groggyRoutine != null)
            StopCoroutine(groggyRoutine);
        groggyRoutine = StartCoroutine(GroggyRoutine(duration));
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        if (!IsDead)
            spriteRenderer.color = normalColor;
        flashRoutine = null;
    }

    private IEnumerator GroggyRoutine(float duration)
    {
        isGroggy = true;
        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = groggyColor;
        yield return new WaitForSeconds(duration);

        if (!IsDead)
        {
            isGroggy = false;
            spriteRenderer.color = normalColor;
        }

        groggyRoutine = null;
    }

    private IEnumerator AttackTimingRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            if (IsDead || isTimeStopped || isGroggy)
                continue;

            isAttackActive = true;
            spriteRenderer.color = attackColor;

            bool hitPlayer = false;
            float activeTimer = 0f;
            while (activeTimer < attackActiveTime)
            {
                activeTimer += Time.deltaTime;
                if (!hitPlayer && TryDamagePlayer())
                    hitPlayer = true;

                yield return null;
            }

            isAttackActive = false;
            if (!IsDead && rb.simulated)
                spriteRenderer.color = normalColor;
        }
    }

    private bool TryDamagePlayer()
    {
        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null || player.IsDead || player.IsInvincible)
            return false;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance > attackRange)
            return false;

        player.TakeDamage(attackDamage);
        return true;
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
        generatedSprite.name = "Generated Dummy Sprite";
        return generatedSprite;
    }
}
