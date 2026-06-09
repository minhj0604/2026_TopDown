using UnityEngine;
using System;

public class ModuleRewardManager : MonoBehaviour
{
    [SerializeField] private ModuleData[] rewardPool;
    [SerializeField] private bool showDebugUI = true;

    private readonly ModuleData[] currentChoices = new ModuleData[3];
    private PlayerModuleInventory targetInventory;
    private Action onRewardTaken;
    private bool isChoosing;
    private bool currentRewardIsElite;
    private ModuleData[] fallbackPool;

    public bool IsChoosing => isChoosing;

    private void Awake()
    {
        CreateFallbackPool();
    }

    public void OfferReward(PlayerModuleInventory inventory, bool isEliteReward, Action onComplete)
    {
        targetInventory = inventory;
        onRewardTaken = onComplete;
        currentRewardIsElite = isEliteReward;

        for (int i = 0; i < currentChoices.Length; i++)
            currentChoices[i] = GetRandomModule();

        isChoosing = true;
    }

    private ModuleData GetRandomModule()
    {
        ModuleData[] pool = rewardPool != null && rewardPool.Length > 0
            ? rewardPool
            : fallbackPool;

        if (pool == null || pool.Length == 0)
            return null;

        for (int i = 0; i < 12; i++)
        {
            ModuleData module = pool[UnityEngine.Random.Range(0, pool.Length)];
            if (module != null)
            {
                ModuleRarity targetRarity = RollRewardRarity(currentRewardIsElite);
                if (module.rarity == targetRarity)
                    return module;
            }
        }

        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }

    private ModuleRarity RollRewardRarity(bool isEliteReward)
    {
        int roll = UnityEngine.Random.Range(0, 100);

        if (isEliteReward)
        {
            if (roll < 45) return ModuleRarity.Rare;
            if (roll < 78) return ModuleRarity.Epic;
            if (roll < 90) return ModuleRarity.Legendary;
            return ModuleRarity.Common;
        }

        if (roll < 65) return ModuleRarity.Common;
        if (roll < 88) return ModuleRarity.Rare;
        if (roll < 98) return ModuleRarity.Epic;
        return ModuleRarity.Legendary;
    }

    private void CreateFallbackPool()
    {
        fallbackPool = new ModuleData[]
        {
            CreateFallbackModule("common_core", "Common Core", ModuleRarity.Common, 1),
            CreateFallbackModule("rare_gear", "Rare Gear", ModuleRarity.Rare, 2),
            CreateFallbackModule("epic_circuit", "Epic Circuit", ModuleRarity.Epic, 3),
            CreateFallbackModule("legendary_clockwork", "Legendary Clockwork", ModuleRarity.Legendary, 4)
        };
    }

    private ModuleData CreateFallbackModule(string id, string moduleName, ModuleRarity rarity, int cost)
    {
        ModuleData module = ScriptableObject.CreateInstance<ModuleData>();
        module.moduleId = id;
        module.moduleName = moduleName;
        module.rarity = rarity;
        module.cost = cost;
        module.description = "Prototype module. Effect will be added later.";
        return module;
    }

    private void TakeReward(int index)
    {
        if (!isChoosing) return;

        ModuleData module = currentChoices[index];
        if (targetInventory != null && module != null)
            targetInventory.AddModule(module);

        isChoosing = false;
        onRewardTaken?.Invoke();
        onRewardTaken = null;
    }

    private void OnGUI()
    {
        if (!showDebugUI || !isChoosing) return;

        GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 90f, 360f, 190f), GUI.skin.box);
        GUILayout.Label("Choose Module Reward");

        for (int i = 0; i < currentChoices.Length; i++)
        {
            ModuleData module = currentChoices[i];
            string label = module != null
                ? $"{module.moduleName} / {module.rarity} / Cost {module.cost}"
                : "Empty Module";

            if (GUILayout.Button(label))
                TakeReward(i);
        }

        GUILayout.EndArea();
    }
}
