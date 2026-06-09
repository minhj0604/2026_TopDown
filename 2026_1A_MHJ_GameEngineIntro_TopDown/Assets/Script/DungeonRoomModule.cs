using UnityEngine;

public class DungeonRoomModule : MonoBehaviour
{
    public DungeonNodeType nodeType = DungeonNodeType.Battle;
    public Transform playerSpawn;
    public DungeonExitDoor exitDoor;

    public Transform GetPlayerSpawn()
    {
        return playerSpawn != null ? playerSpawn : transform;
    }

    public DungeonExitDoor GetExitDoor()
    {
        if (exitDoor != null)
            return exitDoor;

        exitDoor = GetComponentInChildren<DungeonExitDoor>(true);
        return exitDoor;
    }
}
