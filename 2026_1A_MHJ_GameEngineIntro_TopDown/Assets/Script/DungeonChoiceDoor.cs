using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DungeonChoiceDoor : MonoBehaviour
{
    [SerializeField] private DungeonRunManager dungeonRunManager;
    [SerializeField] private bool chooseLeft = true;
    [SerializeField] private Color leftColor = new Color(0.25f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color rightColor = new Color(1f, 0.75f, 0.25f, 0.9f);

    private static Sprite generatedSprite;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (dungeonRunManager == null)
            dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.sortingOrder = 55;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.45f, 0.45f);

        RefreshColor();
    }

    public void Setup(DungeonRunManager manager, bool isLeftDoor)
    {
        dungeonRunManager = manager;
        chooseLeft = isLeftDoor;
        RefreshColor();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dungeonRunManager == null) return;
        if (other.GetComponent<PlayerController>() == null) return;

        if (chooseLeft)
            dungeonRunManager.ChooseLeftNode();
        else
            dungeonRunManager.ChooseRightNode();
    }

    private void RefreshColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = chooseLeft ? leftColor : rightColor;
    }

    private static Sprite GetGeneratedSprite()
    {
        if (generatedSprite != null)
            return generatedSprite;

        Texture2D texture = new Texture2D(18, 18);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[18 * 18];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();

        generatedSprite = Sprite.Create(texture, new Rect(0, 0, 18, 18), new Vector2(0.5f, 0.5f), 18f);
        generatedSprite.name = "Generated Dungeon Choice Door";
        return generatedSprite;
    }
}
