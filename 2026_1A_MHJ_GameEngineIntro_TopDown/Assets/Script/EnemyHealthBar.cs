using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.42f, 0f);
    [SerializeField] private Vector2 size = new Vector2(0.5f, 0.055f);
    [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.8f);
    [SerializeField] private Color fillColor = new Color(0.9f, 0.15f, 0.12f, 0.95f);
    [SerializeField] private int sortingOrder = 120;

    private Transform target;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer fillRenderer;
    private float currentRatio = 1f;

    private void Awake()
    {
        target = transform.parent;
        CreateRenderers();
    }

    private void LateUpdate()
    {
        if (target == null)
            target = transform.parent;
        if (target == null) return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;
        UpdateFillScale();
    }

    public void SetValue(float currentHealth, float maxHealth)
    {
        currentRatio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        bool visible = currentRatio > 0f && currentRatio < 1f;
        if (backgroundRenderer != null)
            backgroundRenderer.enabled = visible;
        if (fillRenderer != null)
            fillRenderer.enabled = visible;

        UpdateFillScale();
    }

    private void CreateRenderers()
    {
        backgroundRenderer = CreateBarPart("Background", backgroundColor, sortingOrder);
        fillRenderer = CreateBarPart("Fill", fillColor, sortingOrder + 1);
        SetValue(1f, 1f);
    }

    private SpriteRenderer CreateBarPart(string objectName, Color color, int order)
    {
        GameObject part = new GameObject(objectName);
        part.transform.SetParent(transform);
        part.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void UpdateFillScale()
    {
        if (backgroundRenderer == null || fillRenderer == null) return;

        backgroundRenderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        fillRenderer.transform.localScale = new Vector3(size.x * currentRatio, size.y, 1f);
        fillRenderer.transform.localPosition = new Vector3(-size.x * (1f - currentRatio) * 0.5f, 0f, -0.01f);
    }

    private static Sprite GetWhiteSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
