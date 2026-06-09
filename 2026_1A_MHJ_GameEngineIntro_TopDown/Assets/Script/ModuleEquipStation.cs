using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ModuleEquipStation : MonoBehaviour
{
    [SerializeField] private Color stationColor = new Color(0.7f, 0.45f, 1f, 0.9f);

    private static Sprite generatedSprite;

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.color = stationColor;
        spriteRenderer.sortingOrder = 60;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.5f, 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerModuleInventory inventory = other.GetComponent<PlayerModuleInventory>();
        if (inventory != null)
            inventory.OpenStation();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerModuleInventory inventory = other.GetComponent<PlayerModuleInventory>();
        if (inventory != null)
            inventory.CloseStation();
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
        generatedSprite.name = "Generated Module Equip Station";
        return generatedSprite;
    }
}
