using UnityEngine;

public class DungeonRoomController : MonoBehaviour
{
    [SerializeField] private DungeonRunManager dungeonRunManager;
    [SerializeField] private DungeonExitDoor exitDoor;
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 playerStartPosition = Vector2.zero;

    private int lastDungeonLevel = -1;
    private int lastNodeIndex = -1;
    private MonoBehaviour[] roomEnemyBehaviours;

    private void Awake()
    {
        if (dungeonRunManager == null)
            dungeonRunManager = FindFirstObjectByType<DungeonRunManager>();
        if (exitDoor == null)
            exitDoor = FindFirstObjectByType<DungeonExitDoor>();
        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
                player = playerController.transform;
        }

        CacheRoomEnemies();
    }

    private void Update()
    {
        if (dungeonRunManager == null || exitDoor == null) return;

        CheckNodeStarted();

        bool shouldOpenDoor = ShouldOpenExitDoor();
        if (exitDoor.gameObject.activeSelf != shouldOpenDoor)
            exitDoor.gameObject.SetActive(shouldOpenDoor);
    }

    private void CheckNodeStarted()
    {
        if (dungeonRunManager.IsWaitingForChoice) return;

        bool isNewNode = lastDungeonLevel != dungeonRunManager.DungeonLevel
            || lastNodeIndex != dungeonRunManager.CurrentNodeIndex;
        if (!isNewNode) return;

        lastDungeonLevel = dungeonRunManager.DungeonLevel;
        lastNodeIndex = dungeonRunManager.CurrentNodeIndex;

        MovePlayerToStart();

        if (dungeonRunManager.IsCurrentNodeCombat)
        {
            SetEnemiesActive(true);
            ResetEnemies();
        }
        else
        {
            SetEnemiesActive(false);
        }
    }

    private void MovePlayerToStart()
    {
        if (player == null) return;

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
            playerBody.position = playerStartPosition;
        else
            player.position = new Vector3(playerStartPosition.x, playerStartPosition.y, player.position.z);
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

    private bool ShouldOpenExitDoor()
    {
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

    private void CacheRoomEnemies()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
}
