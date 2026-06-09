using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class DungeonEntranceDoor : MonoBehaviour
{
    [SerializeField] private DungeonRunManager dungeonRunManager;
    [SerializeField] private Color doorColor = new Color(0.4f, 0.9f, 1f, 0.9f);

    private static Sprite generatedSprite;

    private void Awake()
    {
        if (dungeonRunManager == null)
            dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.color = doorColor;
        spriteRenderer.sortingOrder = 50;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.5f, 0.5f);
    }

    public void SetDungeonRunManager(DungeonRunManager manager)
    {
        dungeonRunManager = manager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dungeonRunManager == null) return;
        if (dungeonRunManager.IsInDungeon) return;
        if (other.GetComponent<PlayerController>() == null) return;

        dungeonRunManager.StartNewRun();
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
        generatedSprite.name = "Generated Dungeon Entrance Door";
        return generatedSprite;
    }
}
