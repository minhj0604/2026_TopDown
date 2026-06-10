using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 7f;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float lifeTimer;
    private static Sprite generatedSprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.065f;

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
    }

    private void OnEnable()
    {
        lifeTimer = lifeTime;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            Destroy(gameObject);
    }

    public void Launch(Vector2 direction, float projectileSpeed, float projectileDamage)
    {
        speed = projectileSpeed;
        damage = projectileDamage;
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        if (playerHealth.IsInvincible)
            return;

        playerHealth.TakeDamage(damage, rb.linearVelocity.normalized);
        Destroy(gameObject);
    }

    private static Sprite GetGeneratedSprite()
    {
        if (generatedSprite != null)
            return generatedSprite;

        Texture2D texture = new Texture2D(12, 12);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[12 * 12];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        generatedSprite = Sprite.Create(texture, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 100f);
        generatedSprite.name = "Generated Enemy Projectile Sprite";
        return generatedSprite;
    }
}
