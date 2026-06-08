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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dungeonRunManager == null) return;
        if (other.GetComponent<PlayerController>() == null) return;

        dungeonRunManager.CompleteCurrentNode();
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
