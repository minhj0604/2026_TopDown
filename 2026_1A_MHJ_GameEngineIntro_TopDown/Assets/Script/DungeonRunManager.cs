using UnityEngine;

public enum DungeonNodeType
{
    Battle,
    Elite,
    Shop,
    Event,
    Boss
}

public class DungeonRunManager : MonoBehaviour
{
    [Header("레벨 디자인 데이터")]
    [SerializeField] private StageData stageData;

    [Header("던전 진행")]
    [SerializeField] private int nodesPerDungeon = 5;
    [SerializeField] private int maxDungeonLevel = 2;
    [SerializeField] private bool showDebugUI = true;

    public int DungeonLevel => dungeonLevel;
    public int CurrentNodeIndex => currentNodeIndex;
    public DungeonNodeType CurrentNodeType => currentNodeType;
    public bool IsWaitingForChoice => waitingForChoice;
    public bool IsRunFinished => isRunFinished;

    public bool IsCurrentNodeCombat
    {
        get
        {
            return currentNodeType == DungeonNodeType.Battle
                || currentNodeType == DungeonNodeType.Elite
                || currentNodeType == DungeonNodeType.Boss;
        }
    }

    private int dungeonLevel = 1;
    private int currentNodeIndex = 0;
    private DungeonNodeType currentNodeType;
    private DungeonNodeType leftChoice;
    private DungeonNodeType rightChoice;
    private bool waitingForChoice = true;
    private bool isRunFinished = false;

    private void Start()
    {
        StartNewRun();
    }

    public void StartNewRun()
    {
        dungeonLevel = 1;
        currentNodeIndex = 0;
        isRunFinished = false;
        CreateNextChoices();
    }

    public void ChooseLeftNode()
    {
        ChooseNode(leftChoice);
    }

    public void ChooseRightNode()
    {
        ChooseNode(rightChoice);
    }

    public void CompleteCurrentNode()
    {
        if (isRunFinished) return;
        if (waitingForChoice) return;

        if (currentNodeType == DungeonNodeType.Boss)
        {
            CompleteBossNode();
            return;
        }

        CreateNextChoices();
    }

    private void ChooseNode(DungeonNodeType nodeType)
    {
        if (isRunFinished) return;
        if (!waitingForChoice) return;

        currentNodeType = nodeType;
        currentNodeIndex++;
        waitingForChoice = false;

        if (SaveDataManager.Instance != null)
            SaveDataManager.Instance.RecordDungeonProgress(dungeonLevel, currentNodeIndex);

        Debug.Log($"Dungeon {dungeonLevel} / Node {currentNodeIndex}: {currentNodeType}", this);
    }

    private void CompleteBossNode()
    {
        if (dungeonLevel >= maxDungeonLevel)
        {
            isRunFinished = true;
            Debug.Log("Dungeon run finished.", this);
            return;
        }

        dungeonLevel++;
        currentNodeIndex = 0;
        CreateNextChoices();
        Debug.Log($"Dungeon changed to level {dungeonLevel}.", this);
    }

    private void CreateNextChoices()
    {
        waitingForChoice = true;

        int nextNodeIndex = currentNodeIndex + 1;
        if (nextNodeIndex >= GetNodesPerDungeon())
        {
            leftChoice = GetBossNodeType();
            rightChoice = GetBossNodeType();
            return;
        }

        if (nextNodeIndex == 1)
        {
            leftChoice = GetFirstNodeType();
            rightChoice = GetFirstNodeType();
            return;
        }

        leftChoice = GetRandomNodeType();
        rightChoice = GetRandomNodeType();

        if (leftChoice == rightChoice)
            rightChoice = GetDifferentNodeType(leftChoice);
    }

    private DungeonNodeType GetRandomNodeType()
    {
        DungeonNodeType scriptedNodeType;
        if (TryGetRandomNodeTypeFromStageData(out scriptedNodeType))
            return scriptedNodeType;

        int randomValue = Random.Range(0, 100);

        if (dungeonLevel <= 1)
        {
            if (randomValue < 60) return DungeonNodeType.Battle;
            if (randomValue < 75) return DungeonNodeType.Event;
            if (randomValue < 90) return DungeonNodeType.Shop;
            return DungeonNodeType.Elite;
        }

        if (randomValue < 45) return DungeonNodeType.Battle;
        if (randomValue < 65) return DungeonNodeType.Elite;
        if (randomValue < 82) return DungeonNodeType.Event;
        return DungeonNodeType.Shop;
    }

    private DungeonNodeType GetDifferentNodeType(DungeonNodeType blockedType)
    {
        for (int i = 0; i < 8; i++)
        {
            DungeonNodeType nodeType = GetRandomNodeType();
            if (nodeType != blockedType)
                return nodeType;
        }

        return blockedType == DungeonNodeType.Battle
            ? DungeonNodeType.Event
            : DungeonNodeType.Battle;
    }

    private int GetNodesPerDungeon()
    {
        if (stageData != null && stageData.nodesPerDungeon > 0)
            return stageData.nodesPerDungeon;

        return nodesPerDungeon;
    }

    private DungeonNodeType GetFirstNodeType()
    {
        if (stageData != null && stageData.firstNode != null)
            return stageData.firstNode.nodeType;

        return DungeonNodeType.Battle;
    }

    private DungeonNodeType GetBossNodeType()
    {
        if (stageData != null && stageData.bossNode != null)
            return stageData.bossNode.nodeType;

        return DungeonNodeType.Boss;
    }

    private bool TryGetRandomNodeTypeFromStageData(out DungeonNodeType nodeType)
    {
        nodeType = DungeonNodeType.Battle;

        if (stageData == null || stageData.randomNodePool == null || stageData.randomNodePool.Length == 0)
            return false;

        for (int i = 0; i < 12; i++)
        {
            NodeData nodeData = stageData.randomNodePool[Random.Range(0, stageData.randomNodePool.Length)];
            if (nodeData == null) continue;

            nodeType = nodeData.nodeType;
            return true;
        }

        return false;
    }

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(20f, 20f, 260f, 180f), GUI.skin.box);
        GUILayout.Label($"Dungeon Level: {dungeonLevel}");
        GUILayout.Label($"Node: {currentNodeIndex} / {GetNodesPerDungeon()}");

        if (isRunFinished)
        {
            GUILayout.Label("Run Clear");
            if (GUILayout.Button("Restart Run"))
                StartNewRun();
        }
        else if (waitingForChoice)
        {
            GUILayout.Label("Choose next node");
            if (leftChoice == rightChoice)
            {
                if (GUILayout.Button(GetNodeDisplayName(leftChoice)))
                    ChooseLeftNode();
            }
            else
            {
                if (GUILayout.Button(GetNodeDisplayName(leftChoice)))
                    ChooseLeftNode();
                if (GUILayout.Button(GetNodeDisplayName(rightChoice)))
                    ChooseRightNode();
            }
        }
        else
        {
            GUILayout.Label($"Current: {GetNodeDisplayName(currentNodeType)}");
            GUILayout.Label("Enter the exit door");
        }

        GUILayout.EndArea();
    }

    private string GetNodeDisplayName(DungeonNodeType nodeType)
    {
        switch (nodeType)
        {
            case DungeonNodeType.Battle: return "Battle Node";
            case DungeonNodeType.Elite: return "Elite Node";
            case DungeonNodeType.Shop: return "Shop Node";
            case DungeonNodeType.Event: return "Event Node";
            case DungeonNodeType.Boss: return "Boss Node";
            default: return nodeType.ToString();
        }
    }
}
