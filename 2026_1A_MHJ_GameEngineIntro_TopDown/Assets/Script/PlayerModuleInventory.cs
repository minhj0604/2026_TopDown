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
    public bool IsStationOpen => stationOpen;
    public int OwnedModuleCount => ownedModules.Count;

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

    public ModuleData GetOwnedModule(int index)
    {
        if (index < 0 || index >= ownedModules.Count)
            return null;

        return ownedModules[index];
    }

    public bool IsEquipped(ModuleData moduleData)
    {
        return moduleData != null && equippedModules.Contains(moduleData);
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

}
