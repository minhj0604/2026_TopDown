using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "TopDown/Level/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageId = "stage_01";
    public string stageName = "Test Dungeon";
    public int nodesPerDungeon = 5;
    public NodeData firstNode;
    public NodeData bossNode;
    public NodeData[] randomNodePool;
}
