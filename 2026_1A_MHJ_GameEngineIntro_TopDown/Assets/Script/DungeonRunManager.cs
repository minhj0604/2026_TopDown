using UnityEngine;
using System.Collections;

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
    [Header("Stage Data")]
    [SerializeField] private StageData stageData;

    [Header("Run")]
    [SerializeField] private int nodesPerDungeon = 15;
    [SerializeField] private int maxDungeonLevel = 3;
    [SerializeField] private int pityChoiceLimit = 5;
    [SerializeField] private bool startInLobby = true;
    [SerializeField] private bool showDebugUI = true;

    [Header("Screen Fade")]
    [SerializeField] private bool useScreenFade = true;
    [SerializeField] private float fadeOutTime = 0.18f;
    [SerializeField] private float fadeHoldTime = 0.08f;
    [SerializeField] private float fadeInTime = 0.18f;

    [Header("Fallback Lobby Door")]
    [SerializeField] private bool createFallbackLobbyDoor = true;
    [SerializeField] private Vector2 fallbackLobbyDoorPosition = new Vector2(0f, 1.2f);
    [SerializeField] private bool createFallbackMaintenanceStation = true;
    [SerializeField] private Vector2 fallbackMaintenancePosition = new Vector2(1.2f, 0f);

    [Header("Fallback Choice Doors")]
    [SerializeField] private bool createFallbackChoiceDoors = false;
    [SerializeField] private Vector2 leftChoiceDoorPosition = new Vector2(-0.45f, 1.2f);
    [SerializeField] private Vector2 rightChoiceDoorPosition = new Vector2(0.45f, 1.2f);

    public int DungeonLevel => dungeonLevel;
    public int CurrentNodeIndex => currentNodeIndex;
    public DungeonNodeType CurrentNodeType => currentNodeType;
    public bool IsInDungeon => isInDungeon;
    public bool IsWaitingForChoice => isInDungeon && waitingForChoice;
    public bool IsRunFinished => isRunFinished;
    public DungeonNodeType LeftChoice => leftChoice;
    public DungeonNodeType RightChoice => rightChoice;
    public int NodesPerDungeon => GetNodesPerDungeon();
    public bool ShowRunResult => showRunResult;
    public bool LastRunCleared => lastRunCleared;
    public int ClearedBattleNodes => clearedBattleNodes;
    public int ClearedEliteNodes => clearedEliteNodes;
    public int ClearedBossNodes => clearedBossNodes;
    public int LastEarnedPermanentCurrency => lastEarnedPermanentCurrency;

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
    private bool waitingForChoice;
    private bool isRunFinished;
    private bool isInDungeon;
    private bool isTransitioning;
    private int choicesSinceShopOrEvent;
    private int clearedBattleNodes;
    private int clearedEliteNodes;
    private int clearedBossNodes;
    private int lastEarnedPermanentCurrency;
    private bool lastRunCleared;
    private bool showRunResult;
    private float fadeAlpha;
    private DungeonEntranceDoor fallbackEntranceDoor;
    private LobbyMaintenanceStation fallbackMaintenanceStation;
    private DungeonChoiceDoor leftChoiceDoor;
    private DungeonChoiceDoor rightChoiceDoor;
    private PlayerWallet playerWallet;

    private void Start()
    {
        EnsureFallbackLobbyDoor();
        EnsureFallbackMaintenanceStation();
        EnsureFallbackChoiceDoors();
        EnsurePlayerPermanentProgress();
        CachePlayerWallet();

        if (startInLobby)
            ReturnToLobbyImmediate();
        else
            StartNewRunImmediate();
    }

    public void StartNewRun()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(StartNewRunImmediate));
    }

    private void StartNewRunImmediate()
    {
        currentNodeIndex = 0;
        ResetRunGold();
        clearedBattleNodes = 0;
        clearedEliteNodes = 0;
        clearedBossNodes = 0;
        lastEarnedPermanentCurrency = 0;
        showRunResult = false;
        isRunFinished = false;
        isInDungeon = true;
        RefreshLobbyDoor();
        RefreshMaintenanceStation();
        CreateNextChoices();
        RefreshChoiceDoors();
    }

    public void ReturnToLobby()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(ReturnToLobbyImmediate));
    }

    private void ReturnToLobbyImmediate()
    {
        isInDungeon = false;
        waitingForChoice = false;
        isRunFinished = false;
        currentNodeIndex = 0;
        ClearRunGold();
        RefreshLobbyDoor();
        RefreshMaintenanceStation();
        RefreshChoiceDoors();
    }

    public void ChooseLeftNode()
    {
        TryChooseNode(leftChoice);
    }

    public void ChooseRightNode()
    {
        TryChooseNode(rightChoice);
    }

    public void ConfirmRunResult()
    {
        showRunResult = false;
    }

    public void CompleteCurrentNode()
    {
        if (isTransitioning) return;
        if (!isInDungeon) return;
        if (isRunFinished) return;
        if (waitingForChoice) return;

        if (currentNodeType == DungeonNodeType.Boss)
        {
            RecordCurrentCombatClear();
            StartCoroutine(TransitionRoutine(CompleteBossNodeImmediate));
            return;
        }

        if (IsCurrentNodeCombat)
            RecordCurrentCombatClear();

        CreateNextChoices();
        RefreshChoiceDoors();
    }

    public GameObject GetCurrentRoomModulePrefab()
    {
        if (stageData == null)
            return null;

        return GetRandomRoomModule(currentNodeType);
    }

    private void TryChooseNode(DungeonNodeType nodeType)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(() => ChooseNodeImmediate(nodeType)));
    }

    private void ChooseNodeImmediate(DungeonNodeType nodeType)
    {
        if (!isInDungeon) return;
        if (isRunFinished) return;
        if (!waitingForChoice) return;

        currentNodeType = nodeType;
        currentNodeIndex++;
        waitingForChoice = false;
        RefreshChoiceDoors();

        if (SaveDataManager.Instance != null)
            SaveDataManager.Instance.RecordDungeonProgress(dungeonLevel, currentNodeIndex);

        Debug.Log($"Dungeon {dungeonLevel} / Node {currentNodeIndex}: {currentNodeType}", this);
    }

    private void CompleteBossNodeImmediate()
    {
        if (dungeonLevel >= maxDungeonLevel)
        {
            isRunFinished = true;
            RefreshChoiceDoors();
            Debug.Log("Dungeon run finished.", this);
            GiveRunResultReward(true);
            ReturnToLobbyImmediate();
            return;
        }

        dungeonLevel++;
        currentNodeIndex = 0;
        Debug.Log($"Stage clear. Next dungeon level: {dungeonLevel}.", this);
        GiveRunResultReward(true);
        ReturnToLobbyImmediate();
    }

    private void CreateNextChoices()
    {
        waitingForChoice = true;

        int nextNodeIndex = currentNodeIndex + 1;
        DungeonNodeType fixedType;
        if (TryGetFixedNodeType(nextNodeIndex, out fixedType))
        {
            leftChoice = fixedType;
            rightChoice = fixedType;
            UpdateShopEventPityCounter();
            return;
        }

        leftChoice = GetRandomNodeType();
        rightChoice = GetRandomNodeType();

        if (leftChoice == rightChoice)
            rightChoice = GetDifferentNodeType(leftChoice);

        ApplyShopEventPity();
        UpdateShopEventPityCounter();
    }

    private bool TryGetFixedNodeType(int nextNodeIndex, out DungeonNodeType nodeType)
    {
        nodeType = DungeonNodeType.Battle;

        if (nextNodeIndex == 1)
        {
            nodeType = GetFirstNodeType();
            return true;
        }

        if (nextNodeIndex == GetFixedShopNodeIndex())
        {
            nodeType = GetShopNodeType();
            return true;
        }

        if (nextNodeIndex >= GetFixedBossNodeIndex())
        {
            nodeType = GetBossNodeType();
            return true;
        }

        return false;
    }

    private DungeonNodeType GetRandomNodeType()
    {
        DungeonNodeType scriptedNodeType;
        if (TryGetRandomNodeTypeFromStageData(out scriptedNodeType))
            return scriptedNodeType;

        int randomValue = Random.Range(0, 100);

        if (dungeonLevel <= 1)
        {
            if (randomValue < 65) return DungeonNodeType.Battle;
            if (randomValue < 82) return DungeonNodeType.Event;
            return DungeonNodeType.Elite;
        }

        if (randomValue < 50) return DungeonNodeType.Battle;
        if (randomValue < 75) return DungeonNodeType.Elite;
        return DungeonNodeType.Event;
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

    private int GetFixedShopNodeIndex()
    {
        if (stageData != null && stageData.fixedShopNodeIndex > 0)
            return stageData.fixedShopNodeIndex;

        return 14;
    }

    private int GetFixedBossNodeIndex()
    {
        if (stageData != null && stageData.fixedBossNodeIndex > 0)
            return stageData.fixedBossNodeIndex;

        return GetNodesPerDungeon();
    }

    private DungeonNodeType GetFirstNodeType()
    {
        if (stageData != null && stageData.firstNode != null)
            return stageData.firstNode.nodeType;

        return DungeonNodeType.Battle;
    }

    private DungeonNodeType GetShopNodeType()
    {
        if (stageData != null && stageData.shopNode != null)
            return stageData.shopNode.nodeType;

        return DungeonNodeType.Shop;
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
            if (nodeData.nodeType == DungeonNodeType.Boss) continue;

            nodeType = nodeData.nodeType;
            return true;
        }

        return false;
    }

    private GameObject GetRandomRoomModule(DungeonNodeType nodeType)
    {
        GameObject[] modules = GetRoomModuleArray(nodeType);
        if (modules == null || modules.Length == 0)
            return null;

        for (int i = 0; i < 12; i++)
        {
            GameObject module = modules[Random.Range(0, modules.Length)];
            if (module != null)
                return module;
        }

        return null;
    }

    private GameObject[] GetRoomModuleArray(DungeonNodeType nodeType)
    {
        switch (nodeType)
        {
            case DungeonNodeType.Battle: return stageData.battleRoomModules;
            case DungeonNodeType.Elite: return stageData.eliteRoomModules;
            case DungeonNodeType.Shop: return stageData.shopRoomModules;
            case DungeonNodeType.Event: return stageData.eventRoomModules;
            case DungeonNodeType.Boss: return stageData.bossRoomModules;
            default: return null;
        }
    }

    private void EnsureFallbackLobbyDoor()
    {
        if (!createFallbackLobbyDoor) return;
        fallbackEntranceDoor = FindFirstObjectByType<DungeonEntranceDoor>();
        if (fallbackEntranceDoor != null) return;

        GameObject doorObject = new GameObject("Dungeon Entrance Door");
        doorObject.transform.position = fallbackLobbyDoorPosition;
        DungeonEntranceDoor door = doorObject.AddComponent<DungeonEntranceDoor>();
        door.SetDungeonRunManager(this);
        fallbackEntranceDoor = door;
    }

    private void EnsureFallbackMaintenanceStation()
    {
        if (!createFallbackMaintenanceStation) return;

        fallbackMaintenanceStation = FindFirstObjectByType<LobbyMaintenanceStation>();
        if (fallbackMaintenanceStation != null) return;

        GameObject stationObject = new GameObject("Lobby Maintenance Station");
        stationObject.transform.position = fallbackMaintenancePosition;
        fallbackMaintenanceStation = stationObject.AddComponent<LobbyMaintenanceStation>();
    }

    private void EnsurePlayerPermanentProgress()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;

        if (playerController.GetComponent<PlayerPermanentProgress>() == null)
            playerController.gameObject.AddComponent<PlayerPermanentProgress>();
    }

    private void CachePlayerWallet()
    {
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null) return;

        playerWallet = playerController.GetComponent<PlayerWallet>();
        if (playerWallet == null)
            playerWallet = playerController.gameObject.AddComponent<PlayerWallet>();
    }

    private void ResetRunGold()
    {
        if (playerWallet == null)
            CachePlayerWallet();
        if (playerWallet != null)
            playerWallet.ResetGoldForRun();
    }

    private void ClearRunGold()
    {
        if (playerWallet == null)
            CachePlayerWallet();
        if (playerWallet != null)
            playerWallet.ClearGold();
    }

    private void RefreshLobbyDoor()
    {
        if (fallbackEntranceDoor == null) return;
        fallbackEntranceDoor.gameObject.SetActive(!isInDungeon);
    }

    private void RefreshMaintenanceStation()
    {
        if (fallbackMaintenanceStation == null) return;
        fallbackMaintenanceStation.gameObject.SetActive(!isInDungeon);
    }

    private void EnsureFallbackChoiceDoors()
    {
        if (!createFallbackChoiceDoors)
        {
            if (leftChoiceDoor != null)
                leftChoiceDoor.gameObject.SetActive(false);
            if (rightChoiceDoor != null)
                rightChoiceDoor.gameObject.SetActive(false);
            return;
        }

        if (leftChoiceDoor == null)
            leftChoiceDoor = CreateChoiceDoor("Left Choice Door", leftChoiceDoorPosition, true);
        if (rightChoiceDoor == null)
            rightChoiceDoor = CreateChoiceDoor("Right Choice Door", rightChoiceDoorPosition, false);

        RefreshChoiceDoors();
    }

    private DungeonChoiceDoor CreateChoiceDoor(string doorName, Vector2 position, bool isLeftDoor)
    {
        GameObject doorObject = new GameObject(doorName);
        doorObject.transform.position = position;
        DungeonChoiceDoor door = doorObject.AddComponent<DungeonChoiceDoor>();
        door.Setup(this, isLeftDoor);
        return door;
    }

    private void RefreshChoiceDoors()
    {
        if (leftChoiceDoor == null || rightChoiceDoor == null)
            return;

        bool shouldShow = isInDungeon && waitingForChoice && !isRunFinished && !isTransitioning;

        leftChoiceDoor.gameObject.SetActive(shouldShow);
        rightChoiceDoor.gameObject.SetActive(shouldShow);
    }

    public void EndRunByDeath()
    {
        if (!isInDungeon) return;
        GiveRunResultReward(false);
        ReturnToLobby();
    }

    private void RecordCurrentCombatClear()
    {
        if (currentNodeType == DungeonNodeType.Battle)
            clearedBattleNodes++;
        else if (currentNodeType == DungeonNodeType.Elite)
            clearedEliteNodes++;
        else if (currentNodeType == DungeonNodeType.Boss)
            clearedBossNodes++;
    }

    private void GiveRunResultReward(bool cleared)
    {
        lastRunCleared = cleared;
        lastEarnedPermanentCurrency = clearedBattleNodes + clearedEliteNodes * 2 + clearedBossNodes * 3;
        if (cleared)
            lastEarnedPermanentCurrency += 5;

        PlayerPermanentProgress progress = FindFirstObjectByType<PlayerPermanentProgress>();
        if (progress != null)
            progress.AddPermanentCurrency(lastEarnedPermanentCurrency);

        showRunResult = true;
    }

    private void ApplyShopEventPity()
    {
        if (choicesSinceShopOrEvent < pityChoiceLimit) return;
        if (leftChoice == DungeonNodeType.Shop || leftChoice == DungeonNodeType.Event) return;
        if (rightChoice == DungeonNodeType.Shop || rightChoice == DungeonNodeType.Event) return;

        rightChoice = UnityEngine.Random.Range(0, 2) == 0
            ? DungeonNodeType.Shop
            : DungeonNodeType.Event;
    }

    private void UpdateShopEventPityCounter()
    {
        if (leftChoice == DungeonNodeType.Shop || leftChoice == DungeonNodeType.Event
            || rightChoice == DungeonNodeType.Shop || rightChoice == DungeonNodeType.Event)
            choicesSinceShopOrEvent = 0;
        else
            choicesSinceShopOrEvent++;
    }

    private IEnumerator TransitionRoutine(System.Action middleAction)
    {
        isTransitioning = true;
        RefreshChoiceDoors();

        if (useScreenFade)
            yield return FadeTo(1f, fadeOutTime);

        if (fadeHoldTime > 0f)
            yield return new WaitForSeconds(fadeHoldTime);

        middleAction?.Invoke();

        if (fadeHoldTime > 0f)
            yield return new WaitForSeconds(fadeHoldTime);

        if (useScreenFade)
            yield return FadeTo(0f, fadeInTime);

        isTransitioning = false;
        RefreshChoiceDoors();
        RefreshLobbyDoor();
        RefreshMaintenanceStation();
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeAlpha;

        if (duration <= 0f)
        {
            fadeAlpha = targetAlpha;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        fadeAlpha = targetAlpha;
    }

    public string GetNodeDisplayName(DungeonNodeType nodeType)
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
