using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int startingGold = 0;
    [SerializeField] private bool showDebugUI = true;

    public int Gold => gold;

    private int gold;

    private void Awake()
    {
        ResetGoldForRun();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        Debug.Log($"Gold +{amount} ({gold})", this);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold < amount) return false;

        gold -= amount;
        Debug.Log($"Gold -{amount} ({gold})", this);
        return true;
    }

    public void ResetGoldForRun()
    {
        gold = Mathf.Max(0, startingGold);
    }

    public void ClearGold()
    {
        gold = 0;
    }

}
