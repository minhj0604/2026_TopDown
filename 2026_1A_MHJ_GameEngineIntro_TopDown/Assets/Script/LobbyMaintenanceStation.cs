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

    private void OnGUI()
    {
        if (!showDebugUI || !stationOpen || currentProgress == null) return;

        float panelWidth = Mathf.Min(520f, Screen.width - 40f);
        GUILayout.BeginArea(new Rect(Screen.width * 0.5f - panelWidth * 0.5f, 20f, panelWidth, 430f), GUI.skin.box);
        GUILayout.Label($"Maintenance / Permanent Currency {currentProgress.PermanentCurrency}");

        int attackCost = currentProgress.GetUpgradeCost(currentProgress.AttackUpgradeLevel);
        if (GUILayout.Button($"Upgrade Character Attack Lv.{currentProgress.AttackUpgradeLevel} / Cost {attackCost}"))
            currentProgress.TryUpgradeAttack();

        int healthCost = currentProgress.GetUpgradeCost(currentProgress.HealthUpgradeLevel);
        if (GUILayout.Button($"Upgrade Character Health Lv.{currentProgress.HealthUpgradeLevel} / Cost {healthCost}"))
            currentProgress.TryUpgradeHealth();

        GUILayout.Space(8f);
        GUILayout.Label("Weapon Loadout");
        if (currentCombat == null)
        {
            GUILayout.Label("No PlayerCombat found");
        }
        else
        {
            GUILayout.Label($"Slot 1 Equipped: {GetWeaponName(currentCombat.weaponSlot1)}");
            GUILayout.Label($"Slot 2 Equipped: {GetWeaponName(currentCombat.weaponSlot2)}");
            GUILayout.Label("Prototype: weapon enhancement tree will attach here later.");

            DrawWeaponSlotButtons(1);
            GUILayout.Space(6f);
            DrawWeaponSlotButtons(2);
        }

        if (GUILayout.Button("Close"))
            stationOpen = false;

        GUILayout.EndArea();
    }

    private string GetWeaponName(WeaponData weapon)
    {
        return weapon != null ? weapon.weaponName : "Empty";
    }

    private void DrawWeaponSlotButtons(int slotNumber)
    {
        GUILayout.Label($"Set Slot {slotNumber}");
        GUILayout.BeginVertical(GUI.skin.box);

        for (int i = 0; i < 3; i++)
        {
            WeaponData candidate = currentCombat.GetLobbyWeaponCandidate(i);
            string label = candidate != null ? candidate.weaponName : "Empty";
            bool selected = currentCombat.GetLobbyWeaponSlotIndex(slotNumber) == i;
            bool usedByOtherSlot = currentCombat.GetLobbyWeaponSlotIndex(slotNumber == 1 ? 2 : 1) == i;

            GUI.enabled = candidate != null && !selected;
            string prefix = selected ? "Selected - " : usedByOtherSlot ? "Swap - " : "";
            if (GUILayout.Button(prefix + label, GUILayout.Height(26f)))
                currentCombat.SetLobbyWeaponSlot(slotNumber, i);
            GUI.enabled = true;
        }

        GUILayout.EndVertical();
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
