using UnityEngine;

public enum WatchSkillType
{
    ParryCounter,
    JustEvadeTimeStop,
    MarkAndBlink
}

[System.Serializable]
public class WeaponComboStep
{
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float knockbackMultiplier = 1f;
    public float groggyTime = 0.06f;
    public float comboDelay = 0.12f;
    public float lungeDistance = 0.08f;
    public float lungeTime = 0.08f;
}

[CreateAssetMenu(fileName = "New WeaponData", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic")]
    public string weaponName;
    public Sprite icon;
    [TextArea(3, 5)]
    public string description;

    [Header("Combat")]
    public float attackPower;
    public float attackSpeed;
    public float attackRange;
    public int comboCount = 3;
    public WeaponComboStep[] comboSteps;

    [Header("Pocket Watch")]
    public WatchSkillType watchSkillType;
    public float gaugeCost;

    [Header("Preview")]
    public GameObject weaponPrefab;

    public WeaponComboStep GetComboStep(int comboStep)
    {
        if (comboSteps == null || comboSteps.Length == 0)
            return null;

        int index = Mathf.Clamp(comboStep - 1, 0, comboSteps.Length - 1);
        return comboSteps[index];
    }

    public float GetComboDamageMultiplier(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0f, step.damageMultiplier) : 1f;
    }

    public float GetComboSpeedMultiplier(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0.1f, step.speedMultiplier) : 1f;
    }

    public float GetComboRangeMultiplier(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0.1f, step.rangeMultiplier) : 1f;
    }

    public float GetComboKnockbackMultiplier(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0f, step.knockbackMultiplier) : 1f;
    }

    public float GetComboGroggyTime(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0f, step.groggyTime) : 0.06f;
    }

    public float GetComboDelay(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0f, step.comboDelay) : 0.12f;
    }

    public float GetComboLungeDistance(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0f, step.lungeDistance) : 0.08f;
    }

    public float GetComboLungeTime(int comboStep)
    {
        WeaponComboStep step = GetComboStep(comboStep);
        return step != null ? Mathf.Max(0.01f, step.lungeTime) : 0.08f;
    }
}
