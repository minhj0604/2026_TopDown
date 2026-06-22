using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DungeonExitDoor : MonoBehaviour
{
    [SerializeField] private DungeonRunManager dungeonRunManager;
    [SerializeField] private Color openColor = new Color(0.2f, 0.9f, 1f, 0.85f);

    private static Sprite generatedSprite;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D doorCollider;
    private bool isOpen;
    private bool hasTriggered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<BoxCollider2D>();

        if (dungeonRunManager == null)
            dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();

        spriteRenderer.color = openColor;
        doorCollider.isTrigger = true;
        SetOpen(gameObject.activeSelf);
    }

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryUseDoor(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryUseDoor(other);
    }

    private void TryUseDoor(Collider2D other)
    {
        if (hasTriggered) return;
        if (!isOpen) return;
        if (dungeonRunManager == null) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;

        hasTriggered = true;
        SetOpen(false);
        dungeonRunManager.CompleteCurrentNode();
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (open)
            hasTriggered = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = open;
        if (doorCollider != null)
            doorCollider.enabled = open;

        gameObject.SetActive(open);
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
        generatedSprite.name = "Generated Door Sprite";
        return generatedSprite;
    }
}
