using System.Collections.Generic;
using UnityEngine;

public class PlayerModuleInventory : MonoBehaviour
{
    [SerializeField] private int maxEquippedCost = 6;
    [SerializeField] private bool showDebugUI = true;

    public int MaxEquippedCost => maxEquippedCost;
    public int CurrentEquippedCost => GetCurrentEquippedCost();

    private readonly List<ModuleData> ownedModules = new List<ModuleData>();
    private readonly List<ModuleData> equippedModules = new List<ModuleData>();
    private bool stationOpen;

    public void AddModule(ModuleData moduleData)
    {
        if (moduleData == null) return;
        ownedModules.Add(moduleData);
        Debug.Log($"Module obtained: {moduleData.moduleName}", this);
    }

    public bool TryEquip(ModuleData moduleData)
    {
        if (moduleData == null) return false;
        if (!ownedModules.Contains(moduleData)) return false;
        if (equippedModules.Contains(moduleData)) return false;
        if (GetCurrentEquippedCost() + moduleData.cost > maxEquippedCost) return false;

        equippedModules.Add(moduleData);
        Debug.Log($"Module equipped: {moduleData.moduleName}", this);
        return true;
    }

    public void Unequip(ModuleData moduleData)
    {
        if (moduleData == null) return;
        equippedModules.Remove(moduleData);
    }

    public void OpenStation()
    {
        stationOpen = true;
    }

    public void CloseStation()
    {
        stationOpen = false;
    }

    private int GetCurrentEquippedCost()
    {
        int totalCost = 0;
        for (int i = 0; i < equippedModules.Count; i++)
        {
            if (equippedModules[i] != null)
                totalCost += equippedModules[i].cost;
        }

        return totalCost;
    }

    private void OnGUI()
    {
        if (!showDebugUI || !stationOpen) return;

        GUILayout.BeginArea(new Rect(Screen.width - 300f, 20f, 280f, 260f), GUI.skin.box);
        GUILayout.Label($"Module Station ({CurrentEquippedCost}/{maxEquippedCost})");

        if (ownedModules.Count == 0)
        {
            GUILayout.Label("No modules owned");
        }
        else
        {
            for (int i = 0; i < ownedModules.Count; i++)
            {
                ModuleData module = ownedModules[i];
                if (module == null) continue;

                bool equipped = equippedModules.Contains(module);
                string label = $"{module.moduleName} / {module.rarity} / Cost {module.cost}";

                if (equipped)
                {
                    if (GUILayout.Button($"Unequip {label}"))
                        Unequip(module);
                }
                else
                {
                    if (GUILayout.Button($"Equip {label}"))
                        TryEquip(module);
                }
            }
        }

        if (GUILayout.Button("Close"))
            CloseStation();

        GUILayout.EndArea();
    }
}
