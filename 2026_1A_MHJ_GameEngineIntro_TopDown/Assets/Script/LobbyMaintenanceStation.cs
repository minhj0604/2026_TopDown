using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class LobbyMaintenanceStation : MonoBehaviour
{
    [SerializeField] private Color stationColor = new Color(0.25f, 0.8f, 1f, 0.95f);
    [SerializeField] private bool showDebugUI = true;

    private static Sprite generatedSprite;
    private PlayerPermanentProgress currentProgress;
    private PlayerCombat currentCombat;
    private bool stationOpen;
    public bool IsOpen => stationOpen;
    public PlayerPermanentProgress CurrentProgress => currentProgress;
    public PlayerCombat CurrentCombat => currentCombat;

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.color = stationColor;
        spriteRenderer.sortingOrder = 65;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.6f, 0.6f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        currentProgress = other.GetComponent<PlayerPermanentProgress>();
        currentCombat = other.GetComponent<PlayerCombat>();
        if (currentProgress != null)
            stationOpen = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerPermanentProgress>() == currentProgress)
        {
            stationOpen = false;
            currentProgress = null;
            currentCombat = null;
        }
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
        generatedSprite.name = "Generated Lobby Maintenance Station";
        return generatedSprite;
    }
}
