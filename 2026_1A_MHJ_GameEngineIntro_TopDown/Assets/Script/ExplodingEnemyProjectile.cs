using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ExplodingEnemyProjectile : MonoBehaviour
{
    [SerializeField] private float damage = 12f;
    [SerializeField] private float speed = 2.2f;
    [SerializeField] private float fuseTime = 1f;
    [SerializeField] private float explosionRadius = 0.75f;
    [SerializeField] private float minimumExplosionRadius = 0.85f;
    [SerializeField] private float explosionVisualScaleMultiplier = 1.35f;
    [SerializeField] private float blinkInterval = 0.12f;
    [SerializeField] private Color normalColor = new Color(1f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color blinkColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color explosionColor = new Color(1f, 0.2f, 0.05f, 1f);
    [SerializeField] private Color explosionRangeColor = new Color(1f, 0.2f, 0.05f, 0.65f);
    [SerializeField] private float explosionRangeLineWidth = 0.035f;
    [SerializeField] private int explosionRangeSegments = 40;

    private static Sprite generatedSprite;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private LineRenderer explosionRangePreview;
    private float fuseTimer;
    private float blinkTimer;
    private bool isBlinkColor;
    private bool exploded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.065f;

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.color = normalColor;
        SetupExplosionRangePreview();
    }

    private void Update()
    {
        if (exploded)
            return;

        UpdateExplosionRangePreview();

        fuseTimer -= Time.deltaTime;
        blinkTimer -= Time.deltaTime;

        if (blinkTimer <= 0f)
        {
            blinkTimer = blinkInterval;
            isBlinkColor = !isBlinkColor;
            spriteRenderer.color = isBlinkColor ? blinkColor : normalColor;
        }

        if (fuseTimer <= 0f)
            Explode();
    }

    public void Launch(Vector2 direction, float projectileSpeed, float projectileDamage)
    {
        Launch(direction, projectileSpeed, projectileDamage, fuseTime, explosionRadius);
    }

    public void Launch(Vector2 direction, float projectileSpeed, float projectileDamage, float fuse, float radius)
    {
        speed = projectileSpeed;
        damage = projectileDamage;
        fuseTime = fuse;
        explosionRadius = Mathf.Max(radius, minimumExplosionRadius);
        fuseTimer = fuseTime;
        blinkTimer = blinkInterval;
        exploded = false;
        UpdateExplosionRangePreview();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploded)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            return;

        if (playerHealth.IsInvincible)
            return;

        Explode();
    }

    private void Explode()
    {
        exploded = true;
        if (explosionRangePreview != null)
            explosionRangePreview.enabled = false;

        rb.linearVelocity = Vector2.zero;
        spriteRenderer.color = explosionColor;
        transform.localScale = Vector3.one * Mathf.Max(0.7f, explosionRadius * explosionVisualScaleMultiplier);

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            Vector2 toPlayer = playerHealth.transform.position - transform.position;
            if (toPlayer.magnitude <= explosionRadius)
                playerHealth.TakeDamage(damage, toPlayer.normalized);
        }

        Destroy(gameObject, 0.08f);
    }

    private void SetupExplosionRangePreview()
    {
        GameObject previewObject = new GameObject("Explosion Range Preview");
        previewObject.transform.SetParent(transform);
        previewObject.transform.localPosition = Vector3.zero;
        explosionRangePreview = previewObject.AddComponent<LineRenderer>();
        explosionRangePreview.useWorldSpace = true;
        explosionRangePreview.loop = true;
        explosionRangePreview.positionCount = explosionRangeSegments;
        explosionRangePreview.startWidth = explosionRangeLineWidth;
        explosionRangePreview.endWidth = explosionRangeLineWidth;
        explosionRangePreview.material = new Material(Shader.Find("Sprites/Default"));
        explosionRangePreview.startColor = explosionRangeColor;
        explosionRangePreview.endColor = explosionRangeColor;
        explosionRangePreview.sortingOrder = 18;
        explosionRangePreview.enabled = false;
    }

    private void UpdateExplosionRangePreview()
    {
        if (explosionRangePreview == null)
            return;

        explosionRangePreview.enabled = true;
        explosionRangePreview.positionCount = explosionRangeSegments;
        for (int i = 0; i < explosionRangeSegments; i++)
        {
            float angle = Mathf.PI * 2f * i / explosionRangeSegments;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * explosionRadius;
            explosionRangePreview.SetPosition(i, point);
        }
    }

    private static Sprite GetGeneratedSprite()
    {
        if (generatedSprite != null)
            return generatedSprite;

        Texture2D texture = new Texture2D(14, 14);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[14 * 14];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        generatedSprite = Sprite.Create(texture, new Rect(0, 0, 14, 14), new Vector2(0.5f, 0.5f), 100f);
        generatedSprite.name = "Generated Exploding Enemy Projectile Sprite";
        return generatedSprite;
    }
}
