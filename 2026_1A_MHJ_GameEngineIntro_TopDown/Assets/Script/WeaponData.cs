using UnityEngine;

public enum WatchSkillType
{
    ParryCounter,
    JustEvadeTimeStop,
    MarkAndBlink
}

[CreateAssetMenu(fileName = "New WeaponData", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("기본 정보")]
    public string weaponName;
    public Sprite icon;
    [TextArea(3, 5)]
    public string description;

    [Header("전투 스탯")]
    public float attackPower;
    public float attackSpeed;
    public float attackRange;

    [Header("회중시계")]
    public WatchSkillType watchSkillType;
    public float gaugeCost;

    [Header("프리팹")]
    public GameObject weaponPrefab;
}
