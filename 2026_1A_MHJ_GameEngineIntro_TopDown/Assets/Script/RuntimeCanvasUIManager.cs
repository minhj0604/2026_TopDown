using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

public class RuntimeCanvasUIManager : MonoBehaviour
{
    public static bool HasInstance { get; private set; }

    private Canvas canvas;
    private Image healthFill;
    private Image clockFill;
    private Image weaponIcon;
    private Text healthText;
    private Text clockText;
    private Text styleText;
    private Text goldText;
    private Text weaponText;
    private Text dungeonText;
    private TMP_Text healthTmpText;
    private TMP_Text clockTmpText;
    private TMP_Text styleTmpText;
    private TMP_Text goldTmpText;
    private TMP_Text weaponTmpText;
    private TMP_Text dungeonTmpText;

    private GameObject lobbyPanel;
    private GameObject nodeChoicePanel;
    private GameObject rewardPanel;
    private GameObject shopPanel;
    private GameObject maintenancePanel;
    private GameObject modulePanel;
    private GameObject resultPanel;

    private Text lobbyTitleText;
    private Button startDungeonButton;
    private Text nodeTitleText;
    private Button leftNodeButton;
    private Button rightNodeButton;
    private Text[] rewardButtonTexts;
    private Button[] rewardButtons;
    private Text shopTitleText;
    private Text healButtonText;
    private Button healButton;
    private Text[] shopButtonTexts;
    private Button[] shopButtons;
    private Text maintenanceTitleText;
    private Text attackUpgradeText;
    private Text healthUpgradeText;
    private Button attackUpgradeButton;
    private Button healthUpgradeButton;
    private Text[] weaponSlotTexts;
    private Button[] weaponSlotButtons;
    private Text moduleTitleText;
    private Text[] moduleButtonTexts;
    private Button[] moduleButtons;
    private Button moduleCloseButton;
    private Text resultText;
    private Button resultConfirmButton;

    private PlayerHealth playerHealth;
    private ClockOutputSystem clockOutput;
    private PlayerWallet wallet;
    private PlayerCombat combat;
    private DungeonRunManager dungeonRunManager;
    private ModuleRewardManager rewardManager;
    private PlayerModuleInventory moduleInventory;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RuntimeCanvasUIManager>() != null)
            return;

        GameObject uiObject = new GameObject("Runtime Canvas UI");
        uiObject.AddComponent<RuntimeCanvasUIManager>();
    }

    private void Awake()
    {
        HasInstance = true;
        BuildCanvas();
        CacheReferences();
    }

    private void OnDestroy()
    {
        if (HasInstance)
            HasInstance = false;
    }

    private void Update()
    {
        CacheReferences();
        UpdateCombatHud();
        UpdateDungeonText();
        UpdatePanels();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Game Canvas UI");
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
        }
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        Transform placedCombatHud = FindTransformByName("CombatHUD");
        if (placedCombatHud == null)
        {
            BuildCombatHud(root);
        }
        else
        {
            placedCombatHud.gameObject.SetActive(true);
            BindPlacedCombatHud(placedCombatHud, root);
        }
        BuildLobbyPanel(root);
        BuildNodeChoicePanel(root);
        BuildRewardPanel(root);
        BuildShopPanel(root);
        BuildMaintenancePanel(root);
        BuildModulePanel(root);
        BuildResultPanel(root);
    }

    private void BuildCombatHud(RectTransform root)
    {
        RectTransform hud = CreatePanel("RuntimeCombatHUD", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 32f), new Vector2(560f, 160f), new Color(0f, 0f, 0f, 0.22f));
        RectTransform watch = CreatePanel("PocketWatchBox", hud, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 18f), new Vector2(118f, 118f), new Color(0.8f, 0.8f, 0.76f, 0.9f));
        CreateText("WATCH", watch, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter);

        RectTransform hpBg = CreatePanel("HealthBar", hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(142f, 96f), new Vector2(380f, 30f), new Color(0.08f, 0.08f, 0.08f, 0.9f));
        healthFill = CreateImage("HealthFill", hpBg, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.85f, 0.12f, 0.12f, 1f));
        healthText = CreateText("HP", hpBg, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter);

        RectTransform clockBg = CreatePanel("ClockGauge", hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(142f, 54f), new Vector2(340f, 26f), new Color(0.08f, 0.08f, 0.08f, 0.9f));
        clockFill = CreateImage("ClockFill", clockBg, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.95f, 0.72f, 0.16f, 1f));
        clockText = CreateText("Clock", clockBg, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero, 15, TextAnchor.MiddleCenter);
        styleText = CreateText("Style D", hud, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(142f, 18f), new Vector2(300f, 24f), 17, TextAnchor.MiddleLeft);

        RectTransform weaponHud = CreatePanel("WeaponHUD", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 52f), new Vector2(120f, 120f), new Color(0f, 0f, 0f, 0.2f));
        RectTransform frame = CreatePanel("WeaponFrame", weaponHud, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(84f, 84f), new Color(0.75f, 0.75f, 0.75f, 0.75f));
        frame.localRotation = Quaternion.Euler(0f, 0f, 45f);
        weaponIcon = CreateImage("WeaponIcon", weaponHud, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(62f, 62f), new Color(0.12f, 0.12f, 0.12f, 0.9f));
        weaponText = CreateText("", weaponHud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -24f), new Vector2(180f, 28f), 14, TextAnchor.MiddleCenter);

        goldText = CreateText("coin: 000", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-190f, 72f), new Vector2(180f, 30f), 18, TextAnchor.MiddleRight);
        dungeonText = CreateText("", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(360f, 120f), 18, TextAnchor.UpperLeft);
    }

    private void BindPlacedCombatHud(Transform placedCombatHud, RectTransform root)
    {
        healthFill = FindUiComponent<Image>(placedCombatHud, "HealthBarFill", "HealthFill", "HealthBarBG (1)");
        clockFill = FindUiComponent<Image>(placedCombatHud, "ClockGaugeFill", "ClockFill", "ClockBarFill");
        weaponIcon = FindUiComponent<Image>(placedCombatHud, "WeaponIcon");

        healthText = FindUiComponent<Text>(placedCombatHud, "HealthText", "HP Text", "HPText");
        clockText = FindUiComponent<Text>(placedCombatHud, "ClockValueText", "ClockText");
        styleText = FindUiComponent<Text>(placedCombatHud, "StyleRankText", "StyleText");
        goldText = FindUiComponent<Text>(placedCombatHud, "GoldText", "CoinText");
        weaponText = FindUiComponent<Text>(placedCombatHud, "WeaponText");
        dungeonText = FindUiComponent<Text>(placedCombatHud, "DungeonText");

        healthTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "HealthText", "HP Text", "HPText");
        clockTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "ClockValueText", "ClockText");
        styleTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "StyleRankText", "StyleText");
        goldTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "GoldText", "CoinText");
        weaponTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "WeaponText");
        dungeonTmpText = FindUiComponent<TMP_Text>(placedCombatHud, "DungeonText");

        if (goldText == null && goldTmpText == null)
            goldText = CreateText("coin: 000", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-190f, 72f), new Vector2(180f, 30f), 18, TextAnchor.MiddleRight);
        if (dungeonText == null && dungeonTmpText == null)
            dungeonText = CreateText("", root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(360f, 120f), 18, TextAnchor.UpperLeft);
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
    }

    private T FindUiComponent<T>(Transform root, params string[] objectNames) where T : Component
    {
        if (root != null)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < objectNames.Length; i++)
            {
                for (int j = 0; j < children.Length; j++)
                {
                    if (children[j].name == objectNames[i])
                    {
                        T component = children[j].GetComponent<T>();
                        if (component != null)
                            return component;
                    }
                }
            }
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            Transform found = FindTransformByName(objectNames[i]);
            if (found == null) continue;

            T component = found.GetComponent<T>();
            if (component != null)
                return component;
        }

        return null;
    }

    private void BuildLobbyPanel(RectTransform root)
    {
        lobbyPanel = CreateCenteredPanel(root, "LobbyPanel", new Vector2(380f, 150f));
        lobbyTitleText = CreateText("Lobby", lobbyPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-24f, 34f), 24, TextAnchor.MiddleCenter);
        startDungeonButton = CreateButton("Start Dungeon", lobbyPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(260f, 46f), () => dungeonRunManager.StartNewRun());
    }

    private void BuildNodeChoicePanel(RectTransform root)
    {
        nodeChoicePanel = CreateCenteredPanel(root, "NodeChoicePanel", new Vector2(560f, 220f));
        nodeTitleText = CreateText("Choose Next Node", nodeChoicePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-24f, 34f), 24, TextAnchor.MiddleCenter);
        leftNodeButton = CreateButton("Left", nodeChoicePanel.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(44f, -20f), new Vector2(220f, 78f), () => dungeonRunManager.ChooseLeftNode());
        rightNodeButton = CreateButton("Right", nodeChoicePanel.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-44f, -20f), new Vector2(220f, 78f), () => dungeonRunManager.ChooseRightNode());
    }

    private void BuildRewardPanel(RectTransform root)
    {
        rewardPanel = CreateCenteredPanel(root, "RewardPanel", new Vector2(750f, 310f));
        CreateText("Choose Module Reward", rewardPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-24f, 36f), 24, TextAnchor.MiddleCenter);
        rewardButtons = new Button[3];
        rewardButtonTexts = new Text[3];
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            int index = i;
            rewardButtons[i] = CreateButton("", rewardPanel.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(-240f + i * 240f, -28f), new Vector2(220f, 180f), () => rewardManager.TakeReward(index));
            rewardButtonTexts[i] = rewardButtons[i].GetComponentInChildren<Text>();
        }
    }

    private void BuildShopPanel(RectTransform root)
    {
        shopPanel = CreateCenteredPanel(root, "ShopPanel", new Vector2(700f, 370f));
        shopTitleText = CreateText("Shop", shopPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-24f, 36f), 24, TextAnchor.MiddleCenter);
        healButton = CreateButton("", shopPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(330f, 42f), () => GetOpenShop()?.TryBuyHeal());
        healButtonText = healButton.GetComponentInChildren<Text>();
        shopButtons = new Button[3];
        shopButtonTexts = new Text[3];
        for (int i = 0; i < shopButtons.Length; i++)
        {
            int index = i;
            shopButtons[i] = CreateButton("", shopPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-230f + i * 230f, -180f), new Vector2(210f, 130f), () => GetOpenShop()?.TryBuy(index));
            shopButtonTexts[i] = shopButtons[i].GetComponentInChildren<Text>();
        }
    }

    private void BuildMaintenancePanel(RectTransform root)
    {
        maintenancePanel = CreateCenteredPanel(root, "MaintenancePanel", new Vector2(780f, 510f));
        maintenanceTitleText = CreateText("Maintenance", maintenancePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-24f, 36f), 24, TextAnchor.MiddleCenter);
        attackUpgradeButton = CreateButton("", maintenancePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(42f, -82f), new Vector2(320f, 44f), () => GetOpenMaintenance()?.CurrentProgress?.TryUpgradeAttack());
        healthUpgradeButton = CreateButton("", maintenancePanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-42f, -82f), new Vector2(320f, 44f), () => GetOpenMaintenance()?.CurrentProgress?.TryUpgradeHealth());
        attackUpgradeText = attackUpgradeButton.GetComponentInChildren<Text>();
        healthUpgradeText = healthUpgradeButton.GetComponentInChildren<Text>();
        weaponSlotButtons = new Button[6];
        weaponSlotTexts = new Text[6];
        for (int slot = 0; slot < 2; slot++)
        {
            CreateText($"Slot {slot + 1}", maintenancePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -150f - slot * 150f), new Vector2(120f, 26f), 18, TextAnchor.MiddleLeft);
            for (int i = 0; i < 3; i++)
            {
                int slotNumber = slot + 1;
                int weaponIndex = i;
                int flat = slot * 3 + i;
                weaponSlotButtons[flat] = CreateButton("", maintenancePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f + i * 190f, -182f - slot * 150f), new Vector2(170f, 82f), () => GetOpenMaintenance()?.CurrentCombat?.SetLobbyWeaponSlot(slotNumber, weaponIndex));
                weaponSlotTexts[flat] = weaponSlotButtons[flat].GetComponentInChildren<Text>();
            }
        }
    }

    private void BuildModulePanel(RectTransform root)
    {
        modulePanel = CreateCenteredPanel(root, "ModulePanel", new Vector2(650f, 430f));
        moduleTitleText = CreateText("Modules", modulePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(-24f, 36f), 24, TextAnchor.MiddleCenter);
        moduleButtons = new Button[6];
        moduleButtonTexts = new Text[6];
        for (int i = 0; i < moduleButtons.Length; i++)
        {
            int index = i;
            moduleButtons[i] = CreateButton("", modulePanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f + (i % 3) * 195f, -86f - (i / 3) * 128f), new Vector2(175f, 108f), () => ToggleModule(index));
            moduleButtonTexts[i] = moduleButtons[i].GetComponentInChildren<Text>();
        }
        moduleCloseButton = CreateButton("Close", modulePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 42f), () => moduleInventory?.CloseStation());
    }

    private void BuildResultPanel(RectTransform root)
    {
        resultPanel = CreateCenteredPanel(root, "RunResultPanel", new Vector2(440f, 290f));
        resultText = CreateText("", resultPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.one * 0.5f, new Vector2(0f, 26f), new Vector2(-44f, -90f), 20, TextAnchor.MiddleCenter);
        resultConfirmButton = CreateButton("Confirm", resultPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 42f), () => dungeonRunManager?.ConfirmRunResult());
    }

    private void CacheReferences()
    {
        playerHealth = playerHealth != null ? playerHealth : FindFirstObjectByType<PlayerHealth>();
        clockOutput = clockOutput != null ? clockOutput : FindFirstObjectByType<ClockOutputSystem>();
        wallet = wallet != null ? wallet : FindFirstObjectByType<PlayerWallet>();
        combat = combat != null ? combat : FindFirstObjectByType<PlayerCombat>();
        dungeonRunManager = dungeonRunManager != null ? dungeonRunManager : FindFirstObjectByType<DungeonRunManager>();
        rewardManager = rewardManager != null ? rewardManager : FindFirstObjectByType<ModuleRewardManager>();
        moduleInventory = moduleInventory != null ? moduleInventory : FindFirstObjectByType<PlayerModuleInventory>();
    }

    private void UpdateCombatHud()
    {
        if (playerHealth != null)
        {
            SetFill(healthFill, playerHealth.CurrentHealth, playerHealth.MaxHealth);
            SetHudText(healthText, healthTmpText, $"HP {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}");
        }
        if (clockOutput != null)
        {
            SetFill(clockFill, clockOutput.CurrentOutput, clockOutput.MaxOutput);
            SetHudText(clockText, clockTmpText, $"{clockOutput.CurrentOutput:0} / {clockOutput.MaxOutput:0}");
            SetHudText(styleText, styleTmpText, $"Style {clockOutput.StyleRankName}  x{clockOutput.StyleMultiplier:0.00}");
        }
        SetHudText(goldText, goldTmpText, wallet != null ? $"coin: {wallet.Gold:000}" : "coin: 000");

        WeaponData weapon = combat != null ? combat.CurrentWeapon : null;
        SetHudText(weaponText, weaponTmpText, weapon != null ? weapon.weaponName : "");
        if (weaponIcon != null)
        {
            weaponIcon.sprite = weapon != null ? weapon.icon : null;
            weaponIcon.enabled = weapon != null && weapon.icon != null;
        }
    }

    private void UpdateDungeonText()
    {
        if (dungeonRunManager == null)
        {
            if (dungeonText != null)
                dungeonText.text = "";
            if (dungeonTmpText != null)
                dungeonTmpText.text = "";
            return;
        }

        if (!dungeonRunManager.IsInDungeon)
        {
            SetHudText(dungeonText, dungeonTmpText, $"Lobby\nNext Stage {dungeonRunManager.DungeonLevel}");
        }
        else if (dungeonRunManager.IsWaitingForChoice)
        {
            SetHudText(dungeonText, dungeonTmpText, $"Stage {dungeonRunManager.DungeonLevel}\nNode {dungeonRunManager.CurrentNodeIndex} / {dungeonRunManager.NodesPerDungeon}\nChoose next node");
        }
        else
        {
            SetHudText(dungeonText, dungeonTmpText, $"Stage {dungeonRunManager.DungeonLevel}\nNode {dungeonRunManager.CurrentNodeIndex} / {dungeonRunManager.NodesPerDungeon}\n{dungeonRunManager.GetNodeDisplayName(dungeonRunManager.CurrentNodeType)}");
        }
    }

    private void UpdatePanels()
    {
        SetActive(lobbyPanel, dungeonRunManager != null && !dungeonRunManager.IsInDungeon && !IsAnyModalOpen());
        SetActive(nodeChoicePanel, dungeonRunManager != null && dungeonRunManager.IsWaitingForChoice && !IsAnyModalOpen());
        SetActive(rewardPanel, rewardManager != null && rewardManager.IsChoosing);

        ShopStation shop = GetOpenShop();
        LobbyMaintenanceStation maintenance = GetOpenMaintenance();
        SetActive(shopPanel, shop != null);
        SetActive(maintenancePanel, maintenance != null);
        SetActive(modulePanel, moduleInventory != null && moduleInventory.IsStationOpen);
        SetActive(resultPanel, dungeonRunManager != null && dungeonRunManager.ShowRunResult);

        UpdateLobbyPanel();
        UpdateNodePanel();
        UpdateRewardPanel();
        UpdateShopPanel(shop);
        UpdateMaintenancePanel(maintenance);
        UpdateModulePanel();
        UpdateResultPanel();
    }

    private bool IsAnyModalOpen()
    {
        return (rewardManager != null && rewardManager.IsChoosing)
            || GetOpenShop() != null
            || GetOpenMaintenance() != null
            || (moduleInventory != null && moduleInventory.IsStationOpen)
            || (dungeonRunManager != null && dungeonRunManager.ShowRunResult);
    }

    private void UpdateLobbyPanel()
    {
        if (dungeonRunManager == null) return;
        lobbyTitleText.text = $"Lobby\nNext Stage {dungeonRunManager.DungeonLevel}";
    }

    private void UpdateNodePanel()
    {
        if (dungeonRunManager == null) return;
        SetButtonLabel(leftNodeButton, dungeonRunManager.GetNodeDisplayName(dungeonRunManager.LeftChoice));
        SetButtonLabel(rightNodeButton, dungeonRunManager.GetNodeDisplayName(dungeonRunManager.RightChoice));
        rightNodeButton.gameObject.SetActive(dungeonRunManager.LeftChoice != dungeonRunManager.RightChoice);
    }

    private void UpdateRewardPanel()
    {
        if (rewardManager == null) return;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            ModuleData module = rewardManager.GetChoice(i);
            rewardButtonTexts[i].text = module != null
                ? $"{module.moduleName}\n{module.rarity} / Cost {module.cost}\n{module.description}"
                : "Empty";
        }
    }

    private void UpdateShopPanel(ShopStation shop)
    {
        if (shop == null) return;

        shopTitleText.text = $"Shop / Gold {shop.Gold}";
        healButtonText.text = $"Heal +{shop.HealAmount:0} HP / {shop.HealPrice}G";
        healButton.interactable = shop.CanHeal;

        for (int i = 0; i < shopButtons.Length; i++)
        {
            ModuleData module = shop.GetShopItem(i);
            bool sold = shop.IsItemSold(i);
            shopButtonTexts[i].text = module != null
                ? $"{(sold ? "Sold\n" : "Buy\n")}{module.moduleName}\n{module.rarity} / {shop.GetItemPrice(i)}G"
                : "Empty";
            shopButtons[i].interactable = !sold && module != null;
        }
    }

    private void UpdateMaintenancePanel(LobbyMaintenanceStation maintenance)
    {
        if (maintenance == null || maintenance.CurrentProgress == null) return;

        PlayerPermanentProgress progress = maintenance.CurrentProgress;
        PlayerCombat playerCombat = maintenance.CurrentCombat;
        maintenanceTitleText.text = $"Maintenance / Currency {progress.PermanentCurrency}";
        attackUpgradeText.text = $"Attack Lv.{progress.AttackUpgradeLevel}\nCost {progress.GetUpgradeCost(progress.AttackUpgradeLevel)}";
        healthUpgradeText.text = $"Health Lv.{progress.HealthUpgradeLevel}\nCost {progress.GetUpgradeCost(progress.HealthUpgradeLevel)}";

        for (int slot = 0; slot < 2; slot++)
        {
            for (int i = 0; i < 3; i++)
            {
                int flat = slot * 3 + i;
                WeaponData weapon = playerCombat != null ? playerCombat.GetLobbyWeaponCandidate(i) : null;
                bool selected = playerCombat != null && playerCombat.GetLobbyWeaponSlotIndex(slot + 1) == i;
                weaponSlotTexts[flat].text = $"{(selected ? "Selected\n" : "")}{(weapon != null ? weapon.weaponName : "Empty")}";
                weaponSlotButtons[flat].interactable = weapon != null && !selected;
            }
        }
    }

    private void UpdateModulePanel()
    {
        if (moduleInventory == null) return;

        moduleTitleText.text = $"Modules ({moduleInventory.CurrentEquippedCost}/{moduleInventory.MaxEquippedCost})";
        for (int i = 0; i < moduleButtons.Length; i++)
        {
            ModuleData module = moduleInventory.GetOwnedModule(i);
            bool hasModule = module != null;
            bool equipped = hasModule && moduleInventory.IsEquipped(module);
            moduleButtonTexts[i].text = hasModule
                ? $"{(equipped ? "Unequip" : "Equip")}\n{module.moduleName}\n{module.rarity} / Cost {module.cost}"
                : "Empty";
            moduleButtons[i].interactable = hasModule;
        }
        moduleCloseButton.interactable = true;
    }

    private void UpdateResultPanel()
    {
        if (dungeonRunManager == null) return;

        resultText.text = $"{(dungeonRunManager.LastRunCleared ? "Run Clear" : "Run Failed")}\n\nBattle Nodes: {dungeonRunManager.ClearedBattleNodes}\nElite Nodes: {dungeonRunManager.ClearedEliteNodes}\nBoss Nodes: {dungeonRunManager.ClearedBossNodes}\nPermanent Currency +{dungeonRunManager.LastEarnedPermanentCurrency}";
        resultConfirmButton.interactable = true;
    }

    private void ToggleModule(int index)
    {
        if (moduleInventory == null) return;

        ModuleData module = moduleInventory.GetOwnedModule(index);
        if (module == null) return;

        if (moduleInventory.IsEquipped(module))
            moduleInventory.Unequip(module);
        else
            moduleInventory.TryEquip(module);
    }

    private ShopStation GetOpenShop()
    {
        ShopStation[] shops = FindObjectsByType<ShopStation>(FindObjectsSortMode.None);
        for (int i = 0; i < shops.Length; i++)
        {
            if (shops[i] != null && shops[i].IsOpen)
                return shops[i];
        }
        return null;
    }

    private LobbyMaintenanceStation GetOpenMaintenance()
    {
        LobbyMaintenanceStation[] stations = FindObjectsByType<LobbyMaintenanceStation>(FindObjectsSortMode.None);
        for (int i = 0; i < stations.Length; i++)
        {
            if (stations[i] != null && stations[i].IsOpen)
                return stations[i];
        }
        return null;
    }

    private GameObject CreateCenteredPanel(RectTransform root, string objectName, Vector2 size)
    {
        RectTransform panel = CreatePanel(objectName, root, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, size, new Color(0.04f, 0.04f, 0.05f, 0.93f));
        return panel.gameObject;
    }

    private RectTransform CreatePanel(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        Image image = CreateImage(objectName, parent, anchorMin, anchorMax, pivot, anchoredPosition, size, color);
        return image.rectTransform;
    }

    private Image CreateImage(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(objectName);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string text, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
    {
        GameObject obj = new GameObject("Text");
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text uiText = obj.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.color = Color.white;
        uiText.alignment = alignment;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        return uiText;
    }

    private Button CreateButton(string label, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreatePanel("Button", parent, anchorMin, anchorMax, pivot, anchoredPosition, size, new Color(0.18f, 0.19f, 0.22f, 0.96f));
        Button button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        CreateText(label, rect, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, new Vector2(-12f, -10f), 16, TextAnchor.MiddleCenter);
        return button;
    }

    private void SetButtonLabel(Button button, string label)
    {
        Text text = button != null ? button.GetComponentInChildren<Text>() : null;
        if (text != null)
            text.text = label;
    }

    private void SetFill(Image image, float current, float max)
    {
        if (image == null) return;

        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = new Vector2(ratio, 1f);
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
    }

    private void SetHudText(Text uiText, TMP_Text tmpText, string value)
    {
        if (uiText != null)
            uiText.text = value;
        if (tmpText != null)
            tmpText.text = value;
    }

    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
            obj.SetActive(active);
    }
}
