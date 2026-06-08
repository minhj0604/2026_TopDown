using UnityEngine;

[CreateAssetMenu(fileName = "NodeData", menuName = "TopDown/Level/Node Data")]
public class NodeData : ScriptableObject
{
    public string nodeId = "node";
    public DungeonNodeType nodeType = DungeonNodeType.Battle;
    public int enemyCount = 2;
    public bool isFixedNode = false;
}
