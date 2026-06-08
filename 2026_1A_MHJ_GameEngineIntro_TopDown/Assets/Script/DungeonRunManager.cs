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
        if (nextNodeIndex >= nodesPerDungeon)
        {
            leftChoice = DungeonNodeType.Boss;
            rightChoice = DungeonNodeType.Boss;
            return;
        }

        if (nextNodeIndex == 1)
        {
            leftChoice = DungeonNodeType.Battle;
            rightChoice = DungeonNodeType.Battle;
            return;
        }

        leftChoice = GetRandomNodeType();
        rightChoice = GetRandomNodeType();

        if (leftChoice == rightChoice)
            rightChoice = GetDifferentNodeType(leftChoice);
    }

    private DungeonNodeType GetRandomNodeType()
    {
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

    private void OnGUI()
    {
        if (!showDebugUI) return;

        GUILayout.BeginArea(new Rect(20f, 20f, 260f, 180f), GUI.skin.box);
        GUILayout.Label($"Dungeon Level: {dungeonLevel}");
        GUILayout.Label($"Node: {currentNodeIndex} / {nodesPerDungeon}");

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
