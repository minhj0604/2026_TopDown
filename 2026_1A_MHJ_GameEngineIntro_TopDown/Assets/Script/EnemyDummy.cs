using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyDummy : MonoBehaviour, IDamageable
{
    [Header("체력")]
    [SerializeField] private float maxHealth = 50f;

    [Header("피격 반응")]
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private Color normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color hitColor = new Color(1f, 0.25f, 0.2f, 1f);

    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;

    private static Sprite generatedSprite;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private float currentHealth;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();

        spriteRenderer.color = normalColor;
    }

    private void Reset()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = false;
        boxCollider.size = Vector2.one;
    }

    public void TakeDamage(float damage, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (hitDirection.sqrMagnitude > 0.01f)
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

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
    }

    private IEnumerator Flash()
    {
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);
        if (!IsDead)
            spriteRenderer.color = normalColor;
        flashRoutine = null;
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
