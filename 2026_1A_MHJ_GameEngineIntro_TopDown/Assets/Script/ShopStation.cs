using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class ShopStation : MonoBehaviour
{
    [SerializeField] private ModuleData[] shopPool;
    [SerializeField] private Color shopColor = new Color(1f, 0.82f, 0.25f, 0.95f);
    [SerializeField] private int healPrice = 25;
    [SerializeField] private float healAmount = 35f;
    [SerializeField] private bool showDebugUI = true;

    private static Sprite generatedSprite;
    private readonly ModuleData[] shopItems = new ModuleData[3];
    private readonly bool[] soldItems = new bool[3];
    private ModuleData[] fallbackPool;
    private PlayerModuleInventory currentInventory;
    private PlayerWallet currentWallet;
    private PlayerHealth currentHealth;
    private bool shopOpen;
    public bool IsOpen => shopOpen;
    public int Gold => currentWallet != null ? currentWallet.Gold : 0;
    public int HealPrice => healPrice;
    public float HealAmount => healAmount;
    public bool CanHeal => currentHealth != null && !currentHealth.IsFullHealth && currentWallet != null && currentWallet.Gold >= healPrice;
    public int ItemCount => shopItems.Length;

    private void Awake()
    {
        CreateFallbackPool();
        RollShopItems();
        SetupVisual();
    }

    private void SetupVisual()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedSprite();
        spriteRenderer.color = shopColor;
        spriteRenderer.sortingOrder = 60;

        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(0.55f, 0.55f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerModuleInventory inventory = other.GetComponent<PlayerModuleInventory>();
        PlayerWallet wallet = other.GetComponent<PlayerWallet>();
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (inventory == null || wallet == null) return;

        currentInventory = inventory;
        currentWallet = wallet;
        currentHealth = health;
        shopOpen = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<PlayerModuleInventory>() == currentInventory)
        {
            shopOpen = false;
            currentInventory = null;
            currentWallet = null;
            currentHealth = null;
        }
    }

    private void RollShopItems()
    {
        for (int i = 0; i < shopItems.Length; i++)
        {
            shopItems[i] = GetRandomShopModule();
            soldItems[i] = false;
        }
    }

    private ModuleData GetRandomShopModule()
    {
        ModuleData[] pool = shopPool != null && shopPool.Length > 0
            ? shopPool
            : fallbackPool;

        if (pool == null || pool.Length == 0)
            return null;

        for (int i = 0; i < 12; i++)
        {
            ModuleData module = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (module != null)
                return module;
        }

        return null;
    }

    public ModuleData GetShopItem(int index)
    {
        if (index < 0 || index >= shopItems.Length)
            return null;

        return shopItems[index];
    }

    public bool IsItemSold(int index)
    {
        return index < 0 || index >= soldItems.Length || soldItems[index];
    }

    public int GetItemPrice(int index)
    {
        return GetPrice(GetShopItem(index));
    }

    private int GetPrice(ModuleData module)
    {
        if (module == null) return 0;

        int rarityPrice = 0;
        switch (module.rarity)
        {
            case ModuleRarity.Common: rarityPrice = 25; break;
            case ModuleRarity.Rare: rarityPrice = 45; break;
            case ModuleRarity.Epic: rarityPrice = 75; break;
            case ModuleRarity.Legendary: rarityPrice = 120; break;
        }

        return rarityPrice + module.cost * 5;
    }

    public void TryBuy(int index)
    {
        if (index < 0 || index >= shopItems.Length) return;
        if (currentInventory == null || currentWallet == null) return;
        if (soldItems[index]) return;

        ModuleData module = shopItems[index];
        int price = GetPrice(module);
        if (!currentWallet.TrySpendGold(price)) return;

        currentInventory.AddModule(module);
        soldItems[index] = true;
    }

    public void TryBuyHeal()
    {
        if (currentWallet == null || currentHealth == null) return;
        if (currentHealth.IsFullHealth) return;
        if (!currentWallet.TrySpendGold(healPrice)) return;

        currentHealth.TryHeal(healAmount);
    }

    private void CreateFallbackPool()
    {
        fallbackPool = new ModuleData[]
        {
            CreateFallbackModule("shop_common_core", "Common Shop Core", ModuleRarity.Common, 1),
            CreateFallbackModule("shop_rare_gear", "Rare Shop Gear", ModuleRarity.Rare, 2),
            CreateFallbackModule("shop_epic_circuit", "Epic Shop Circuit", ModuleRarity.Epic, 3),
            CreateFallbackModule("shop_legendary_clockwork", "Legendary Shop Clockwork", ModuleRarity.Legendary, 4)
        };
    }

    private ModuleData CreateFallbackModule(string id, string moduleName, ModuleRarity rarity, int cost)
    {
        ModuleData module = ScriptableObject.CreateInstance<ModuleData>();
        module.moduleId = id;
        module.moduleName = moduleName;
        module.rarity = rarity;
        module.cost = cost;
        module.description = "Shop prototype module. Effect will be added later.";
        return module;
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
        generatedSprite.name = "Generated Shop Station";
        return generatedSprite;
    }
}
