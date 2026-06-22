using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class DungeonRoomController : MonoBehaviour
{
    [SerializeField] private DungeonRunManager dungeonRunManager;
    [SerializeField] private DungeonExitDoor fallbackExitDoor;
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 fallbackPlayerStartPosition = Vector2.zero;
    [SerializeField] private Transform roomModuleParent;
    [SerializeField] private bool buildGeneratedRoomWhenNoPrefab = true;
    [SerializeField] private int generatedRoomWidth = 10;
    [SerializeField] private int generatedRoomHeight = 7;
    [SerializeField] private float nonCombatCompleteDelay = 0.35f;
    [SerializeField] private float combatClearSlowScale = 0.25f;
    [SerializeField] private float combatClearSlowTime = 2.2f;
    [SerializeField] private float combatClearZoomOutAmount = 0.18f;
    [SerializeField] private float combatClearZoomOutTime = 2.2f;
    [SerializeField] private float combatClearZoomReturnTime = 0.28f;

    private int lastDungeonLevel = -1;
    private int lastNodeIndex = -1;
    private bool lastInDungeon;
    private GameObject activeRoomModule;
    private MonoBehaviour[] roomEnemyBehaviours = new MonoBehaviour[0];
    private bool currentNodeCompleted;
    private float nonCombatCompleteTimer;
    private ModuleRewardManager moduleRewardManager;
    private PlayerModuleInventory playerModuleInventory;
    private PlayerWallet playerWallet;

    private void Awake()
    {
        if (dungeonRunManager == null)
            dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();
        if (fallbackExitDoor == null)
            fallbackExitDoor = FindFirstObjectByType<DungeonExitDoor>(FindObjectsInactive.Include);
        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
                player = playerController.transform;
        }
        if (player != null)
        {
            playerModuleInventory = player.GetComponent<PlayerModuleInventory>();
            if (playerModuleInventory == null)
                playerModuleInventory = player.gameObject.AddComponent<PlayerModuleInventory>();

            playerWallet = player.GetComponent<PlayerWallet>();
            if (playerWallet == null)
                playerWallet = player.gameObject.AddComponent<PlayerWallet>();
        }

        if (roomModuleParent == null)
            roomModuleParent = transform;

        moduleRewardManager = FindFirstObjectByType<ModuleRewardManager>();
        if (moduleRewardManager == null)
            moduleRewardManager = gameObject.AddComponent<ModuleRewardManager>();

        CacheFallbackRoomEnemies();
        SetCurrentRoomActive(false);
        DisableAllExitDoors();
    }

    private void Start()
    {
        DisableAllExitDoors();
    }

    private void Update()
    {
        if (dungeonRunManager == null) return;

        DisableAllExitDoors();
        CheckLobbyState();
        CheckNodeStarted();
        CheckNodeCompleted();
    }

    private void CheckLobbyState()
    {
        if (lastInDungeon == dungeonRunManager.IsInDungeon)
            return;

        lastInDungeon = dungeonRunManager.IsInDungeon;

        if (!lastInDungeon)
        {
            ClearActiveRoomModule();
            CacheFallbackRoomEnemies();
            SetCurrentRoomActive(false);
            DisableAllExitDoors();
            lastDungeonLevel = -1;
            lastNodeIndex = -1;
        }
    }

    private void CheckNodeStarted()
    {
        if (!dungeonRunManager.IsInDungeon) return;
        if (dungeonRunManager.IsWaitingForChoice) return;

        bool isNewNode = lastDungeonLevel != dungeonRunManager.DungeonLevel
            || lastNodeIndex != dungeonRunManager.CurrentNodeIndex;
        if (!isNewNode) return;

        lastDungeonLevel = dungeonRunManager.DungeonLevel;
        lastNodeIndex = dungeonRunManager.CurrentNodeIndex;

        BuildRoomForCurrentNode();
        MovePlayerToStart();
        currentNodeCompleted = false;
        nonCombatCompleteTimer = nonCombatCompleteDelay;

        if (dungeonRunManager.IsCurrentNodeCombat)
        {
            SetEnemiesActive(true);
            ResetEnemies();
        }
        else
        {
            SetEnemiesActive(false);
        }
        DisableAllExitDoors();
    }

    private void BuildRoomForCurrentNode()
    {
        ClearActiveRoomModule();

        GameObject modulePrefab = dungeonRunManager.GetCurrentRoomModulePrefab();
        if (modulePrefab == null)
        {
            if (buildGeneratedRoomWhenNoPrefab)
            {
                activeRoomModule = BuildGeneratedRoomModule();
                CacheRoomEnemiesFrom(activeRoomModule);
                DisableExitDoorsIn(activeRoomModule);
            }
            else
            {
                CacheFallbackRoomEnemies();
                SetCurrentRoomActive(true);
                DisableAllExitDoors();
            }
            return;
        }

        activeRoomModule = Instantiate(modulePrefab, roomModuleParent);
        activeRoomModule.transform.localPosition = Vector3.zero;

        DungeonRoomModule roomModule = activeRoomModule.GetComponent<DungeonRoomModule>();
        CacheRoomEnemiesFrom(activeRoomModule);
        DisableExitDoorsIn(activeRoomModule);
    }

    private void ClearActiveRoomModule()
    {
        if (activeRoomModule != null)
            Destroy(activeRoomModule);

        activeRoomModule = null;
    }

    private void MovePlayerToStart()
    {
        if (player == null) return;

        Vector2 startPosition = fallbackPlayerStartPosition;
        if (activeRoomModule != null)
        {
            DungeonRoomModule roomModule = activeRoomModule.GetComponent<DungeonRoomModule>();
            if (roomModule != null && roomModule.GetPlayerSpawn() != null)
                startPosition = roomModule.GetPlayerSpawn().position;
        }

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
            playerBody.position = startPosition;
        else
            player.position = new Vector3(startPosition.x, startPosition.y, player.position.z);
    }

    private void CheckNodeCompleted()
    {
        if (currentNodeCompleted) return;
        if (moduleRewardManager != null && moduleRewardManager.IsChoosing) return;
        if (!dungeonRunManager.IsInDungeon) return;
        if (dungeonRunManager.IsRunFinished) return;
        if (dungeonRunManager.IsWaitingForChoice) return;
        if (!ShouldCompleteCurrentNode()) return;

        if (!dungeonRunManager.IsCurrentNodeCombat && nonCombatCompleteTimer > 0f)
        {
            nonCombatCompleteTimer -= Time.deltaTime;
            return;
        }

        currentNodeCompleted = true;

        if (dungeonRunManager.IsCurrentNodeCombat && moduleRewardManager != null)
        {
            StartCoroutine(CombatClearRoutine());
        }
        else
        {
            dungeonRunManager.CompleteCurrentNode();
        }
    }

    private IEnumerator CombatClearRoutine()
    {
        GiveCombatClearGold();
        float clearSlowTime = Mathf.Max(2.2f, combatClearSlowTime);
        float clearSlowScale = Mathf.Clamp(combatClearSlowScale, 0.08f, 0.35f);
        PlayCombatClearCameraEffect(clearSlowTime);

        GameTimeScaleController.RequestSlowMotion(clearSlowScale, clearSlowTime);
        yield return new WaitForSecondsRealtime(clearSlowTime);

        if (moduleRewardManager != null)
        {
            moduleRewardManager.OfferReward(
                playerModuleInventory,
                dungeonRunManager.CurrentNodeType == DungeonNodeType.Elite,
                dungeonRunManager.CompleteCurrentNode);
        }
        else
        {
            dungeonRunManager.CompleteCurrentNode();
        }
    }

    private void PlayCombatClearCameraEffect(float clearSlowTime)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        SimpleCameraShake cameraControl = mainCamera.GetComponent<SimpleCameraShake>();
        if (cameraControl == null)
            cameraControl = mainCamera.gameObject.AddComponent<SimpleCameraShake>();

        float zoomOutAmount = Mathf.Min(combatClearZoomOutAmount, 0.18f);
        float zoomOutTime = Mathf.Max(clearSlowTime, combatClearZoomOutTime);
        float zoomReturnTime = Mathf.Clamp(combatClearZoomReturnTime, 0.16f, 0.35f);

        cameraControl.PlayRoomClearZoomOut(
            zoomOutAmount,
            zoomOutTime,
            0f,
            zoomReturnTime);
    }

    private void GiveCombatClearGold()
    {
        if (playerWallet == null) return;

        int goldAmount = 15;
        if (dungeonRunManager.CurrentNodeType == DungeonNodeType.Elite)
            goldAmount = 25;
        else if (dungeonRunManager.CurrentNodeType == DungeonNodeType.Boss)
            goldAmount = 50;

        playerWallet.AddGold(goldAmount);
    }

    private void SetCurrentRoomActive(bool isActive)
    {
        if (fallbackExitDoor != null)
            fallbackExitDoor.SetOpen(false);
        SetEnemiesActive(isActive);
    }

    private void DisableAllExitDoors()
    {
        DungeonExitDoor[] doors = FindObjectsByType<DungeonExitDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
                doors[i].SetOpen(false);
        }
    }

    private void DisableExitDoorsIn(GameObject root)
    {
        if (root == null) return;

        DungeonExitDoor[] doors = root.GetComponentsInChildren<DungeonExitDoor>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            if (doors[i] != null)
                doors[i].SetOpen(false);
        }
    }

    private void SetEnemiesActive(bool isActive)
    {
        for (int i = 0; i < roomEnemyBehaviours.Length; i++)
        {
            if (roomEnemyBehaviours[i] != null)
                roomEnemyBehaviours[i].gameObject.SetActive(isActive);
        }
    }

    private void ResetEnemies()
    {
        for (int i = 0; i < roomEnemyBehaviours.Length; i++)
        {
            IRoomEnemy roomEnemy = roomEnemyBehaviours[i] as IRoomEnemy;
            if (roomEnemy != null)
                roomEnemy.ResetEnemy();
        }
    }

    private bool ShouldCompleteCurrentNode()
    {
        if (!dungeonRunManager.IsInDungeon) return false;
        if (dungeonRunManager.IsRunFinished) return false;
        if (dungeonRunManager.IsWaitingForChoice) return false;
        if (!dungeonRunManager.IsCurrentNodeCombat) return true;

        for (int i = 0; i < roomEnemyBehaviours.Length; i++)
        {
            MonoBehaviour enemyBehaviour = roomEnemyBehaviours[i];
            IRoomEnemy roomEnemy = enemyBehaviour as IRoomEnemy;
            if (roomEnemy != null && enemyBehaviour.gameObject.activeSelf && !roomEnemy.IsDead)
                return false;
        }

        return true;
    }

    private void CacheFallbackRoomEnemies()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        CacheEnemiesFromBehaviours(behaviours);
    }

    private void CacheRoomEnemiesFrom(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        CacheEnemiesFromBehaviours(behaviours);
    }

    private void CacheEnemiesFromBehaviours(MonoBehaviour[] behaviours)
    {
        int enemyCount = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRoomEnemy)
                enemyCount++;
        }

        roomEnemyBehaviours = new MonoBehaviour[enemyCount];
        int index = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRoomEnemy)
            {
                roomEnemyBehaviours[index] = behaviours[i];
                index++;
            }
        }
    }

    private GameObject BuildGeneratedRoomModule()
    {
        GameObject roomObject = new GameObject($"Generated Room - {dungeonRunManager.CurrentNodeType}");
        roomObject.transform.SetParent(roomModuleParent);
        roomObject.transform.localPosition = Vector3.zero;

        DungeonRoomModule roomModule = roomObject.AddComponent<DungeonRoomModule>();
        roomModule.nodeType = dungeonRunManager.CurrentNodeType;

        BuildGeneratedTilemap(roomObject.transform, dungeonRunManager.CurrentNodeType);
        roomModule.playerSpawn = CreateMarker(roomObject.transform, "Player Spawn", new Vector2(0f, -2f));

        roomModule.exitDoor = null;

        if (!dungeonRunManager.IsCurrentNodeCombat)
            CreateModuleEquipStation(roomObject.transform);

        if (dungeonRunManager.CurrentNodeType == DungeonNodeType.Shop)
            CreateShopStation(roomObject.transform);

        SpawnGeneratedEnemies(roomObject.transform, dungeonRunManager.CurrentNodeType);
        return roomObject;
    }

    private void BuildGeneratedTilemap(Transform parent, DungeonNodeType nodeType)
    {
        GameObject gridObject = new GameObject("Grid");
        gridObject.transform.SetParent(parent);
        gridObject.transform.localPosition = Vector3.zero;
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        GameObject floorObject = new GameObject("Generated Tilemap");
        floorObject.transform.SetParent(gridObject.transform);
        floorObject.transform.localPosition = Vector3.zero;

        Tilemap tilemap = floorObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = floorObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -10;

        Tile floorTile = CreateTile(GetFloorColor(nodeType));
        Tile wallTile = CreateTile(new Color(0.22f, 0.22f, 0.26f, 1f));

        int halfWidth = generatedRoomWidth / 2;
        int halfHeight = generatedRoomHeight / 2;

        for (int x = -halfWidth; x <= halfWidth; x++)
        {
            for (int y = -halfHeight; y <= halfHeight; y++)
            {
                bool isWall = x == -halfWidth || x == halfWidth || y == -halfHeight || y == halfHeight;
                tilemap.SetTile(new Vector3Int(x, y, 0), isWall ? wallTile : floorTile);
            }
        }
    }

    private Tile CreateTile(Color color)
    {
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = CreateSquareSprite(color);
        tile.color = Color.white;
        return tile;
    }

    private Sprite CreateSquareSprite(Color color)
    {
        const int size = 18;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Color GetFloorColor(DungeonNodeType nodeType)
    {
        switch (nodeType)
        {
            case DungeonNodeType.Shop: return new Color(0.18f, 0.25f, 0.28f, 1f);
            case DungeonNodeType.Event: return new Color(0.22f, 0.18f, 0.28f, 1f);
            case DungeonNodeType.Elite: return new Color(0.28f, 0.18f, 0.18f, 1f);
            case DungeonNodeType.Boss: return new Color(0.18f, 0.12f, 0.16f, 1f);
            default: return new Color(0.16f, 0.18f, 0.2f, 1f);
        }
    }

    private Transform CreateMarker(Transform parent, string markerName, Vector2 position)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent);
        marker.transform.localPosition = position;
        return marker.transform;
    }

    private void CreateModuleEquipStation(Transform parent)
    {
        GameObject stationObject = new GameObject("Module Equip Station");
        stationObject.transform.SetParent(parent);
        stationObject.transform.localPosition = dungeonRunManager.CurrentNodeType == DungeonNodeType.Shop
            ? new Vector2(-0.8f, 0.4f)
            : new Vector2(0f, 0.4f);
        stationObject.AddComponent<ModuleEquipStation>();
    }

    private void CreateShopStation(Transform parent)
    {
        GameObject stationObject = new GameObject("Shop Station");
        stationObject.transform.SetParent(parent);
        stationObject.transform.localPosition = new Vector2(0.8f, 0.4f);
        stationObject.AddComponent<ShopStation>();
    }

    private void SpawnGeneratedEnemies(Transform parent, DungeonNodeType nodeType)
    {
        if (nodeType == DungeonNodeType.Shop || nodeType == DungeonNodeType.Event)
            return;

        if (nodeType == DungeonNodeType.Boss)
        {
            CreateEnemy(parent, "Boss Dash Cone", typeof(DashConeEnemy), new Vector2(0f, 1.65f), new Vector3(1.35f, 1.35f, 1f));
            SpawnRandomEnemySet(parent, GetEliteEnemyPool(), new Vector2[] { new Vector2(-2.4f, 0.45f), new Vector2(2.4f, 0.45f) }, 2, "Boss Support");
            return;
        }

        if (nodeType == DungeonNodeType.Elite)
        {
            SpawnRandomEnemySet(parent, GetEliteEnemyPool(), GetEliteEnemyPositions(), 3, "Elite Enemy");
            return;
        }

        SpawnRandomEnemySet(parent, GetCommonEnemyPool(), GetCommonEnemyPositions(), 3, "Enemy");
    }

    private void SpawnRandomEnemySet(Transform parent, System.Type[] enemyPool, Vector2[] positions, int count, string namePrefix)
    {
        int spawnCount = Mathf.Min(count, positions.Length);
        bool[] usedPositions = new bool[positions.Length];
        bool hasBarrageEnemy = false;
        for (int i = 0; i < spawnCount; i++)
        {
            System.Type enemyType = GetRandomEnemyType(enemyPool, !hasBarrageEnemy);
            if (IsBarrageEnemyType(enemyType))
                hasBarrageEnemy = true;

            Vector2 position = GetRandomUnusedPosition(positions, usedPositions);
            CreateEnemy(parent, $"{namePrefix} {i + 1}", enemyType, position, Vector3.one);
        }
    }

    private Vector2 GetRandomUnusedPosition(Vector2[] positions, bool[] usedPositions)
    {
        for (int attempt = 0; attempt < 12; attempt++)
        {
            int index = UnityEngine.Random.Range(0, positions.Length);
            if (usedPositions[index])
                continue;

            usedPositions[index] = true;
            return positions[index];
        }

        for (int i = 0; i < positions.Length; i++)
        {
            if (usedPositions[i])
                continue;

            usedPositions[i] = true;
            return positions[i];
        }

        return positions[0];
    }

    private System.Type GetRandomEnemyType(System.Type[] enemyPool, bool allowBarrageEnemy = true)
    {
        if (enemyPool == null || enemyPool.Length == 0)
            return typeof(ChaserEnemy);

        for (int attempt = 0; attempt < 12; attempt++)
        {
            System.Type enemyType = enemyPool[UnityEngine.Random.Range(0, enemyPool.Length)];
            if (allowBarrageEnemy || !IsBarrageEnemyType(enemyType))
                return enemyType;
        }

        for (int i = 0; i < enemyPool.Length; i++)
        {
            if (!IsBarrageEnemyType(enemyPool[i]))
                return enemyPool[i];
        }

        return enemyPool[0];
    }

    private bool IsBarrageEnemyType(System.Type enemyType)
    {
        return enemyType == typeof(ShooterEnemy)
            || enemyType == typeof(FixedBarrageEnemy)
            || enemyType == typeof(BombThrowerEnemy);
    }

    private System.Type[] GetCommonEnemyPool()
    {
        return new System.Type[]
        {
            typeof(ChaserEnemy),
            typeof(ShooterEnemy),
            typeof(BombThrowerEnemy),
            typeof(LineStrikeEnemy)
        };
    }

    private System.Type[] GetEliteEnemyPool()
    {
        return new System.Type[]
        {
            typeof(ChargerEnemy),
            typeof(DashConeEnemy),
            typeof(BombThrowerEnemy),
            typeof(LineStrikeEnemy),
            typeof(FixedBarrageEnemy)
        };
    }

    private Vector2[] GetCommonEnemyPositions()
    {
        return new Vector2[]
        {
            new Vector2(-2.4f, 1.35f),
            new Vector2(2.4f, 1.35f),
            new Vector2(0f, 2f),
            new Vector2(-2.1f, -0.1f),
            new Vector2(2.1f, -0.1f)
        };
    }

    private Vector2[] GetEliteEnemyPositions()
    {
        return new Vector2[]
        {
            new Vector2(0f, 1.75f),
            new Vector2(-2.5f, 0.45f),
            new Vector2(2.5f, 0.45f),
            new Vector2(-1.4f, 2.15f),
            new Vector2(1.4f, 2.15f)
        };
    }

    private void CreateEnemy(Transform parent, string enemyName, System.Type enemyType, Vector2 position, Vector3 scale)
    {
        GameObject enemyObject = new GameObject(enemyName);
        enemyObject.transform.SetParent(parent);
        enemyObject.transform.localPosition = position;
        enemyObject.transform.localScale = scale;
        enemyObject.AddComponent<SpriteRenderer>();
        enemyObject.AddComponent<BoxCollider2D>();
        enemyObject.AddComponent<Rigidbody2D>();
        enemyObject.AddComponent(enemyType);
    }
}
