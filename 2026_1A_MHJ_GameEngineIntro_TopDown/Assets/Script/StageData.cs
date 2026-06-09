using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "TopDown/Level/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageId = "stage_01";
    public string stageName = "Test Dungeon";
    public int nodesPerDungeon = 15;
    public int fixedShopNodeIndex = 14;
    public int fixedBossNodeIndex = 15;
    public NodeData firstNode;
    public NodeData shopNode;
    public NodeData bossNode;
    public NodeData[] randomNodePool;

    [Header("Room Module Prefabs")]
    public GameObject[] battleRoomModules;
    public GameObject[] eliteRoomModules;
    public GameObject[] shopRoomModules;
    public GameObject[] eventRoomModules;
    public GameObject[] bossRoomModules;
}
