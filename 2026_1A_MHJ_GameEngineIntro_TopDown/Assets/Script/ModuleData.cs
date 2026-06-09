using UnityEngine;

public enum ModuleRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "ModuleData", menuName = "TopDown/Module/Module Data")]
public class ModuleData : ScriptableObject
{
    public string moduleId = "module";
    public string moduleName = "New Module";
    public ModuleRarity rarity = ModuleRarity.Common;
    public int cost = 1;
    [TextArea(2, 4)]
    public string description;
}
