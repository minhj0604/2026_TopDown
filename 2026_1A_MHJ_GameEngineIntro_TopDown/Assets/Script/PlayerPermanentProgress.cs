using UnityEngine;

public class PlayerPermanentProgress : MonoBehaviour
{
    [SerializeField] private int permanentCurrency;
    [SerializeField] private int attackUpgradeLevel;
    [SerializeField] private int healthUpgradeLevel;
    [SerializeField] private int upgradeCostBase = 5;

    public int PermanentCurrency => permanentCurrency;
    public int AttackUpgradeLevel => attackUpgradeLevel;
    public int HealthUpgradeLevel => healthUpgradeLevel;
    public float AttackDamageMultiplier => 1f + attackUpgradeLevel * 0.08f;

    private void Start()
    {
        LoadFromSave();
    }

    public void AddPermanentCurrency(int amount)
    {
        if (amount <= 0) return;
        permanentCurrency += amount;
        SaveProgress();
        Debug.Log($"Permanent currency +{amount} ({permanentCurrency})", this);
    }

    public bool TryUpgradeAttack()
    {
        int cost = GetUpgradeCost(attackUpgradeLevel);
        if (permanentCurrency < cost) return false;

        permanentCurrency -= cost;
        attackUpgradeLevel++;
        SaveProgress();
        Debug.Log($"Attack upgrade level {attackUpgradeLevel}", this);
        return true;
    }

    public bool TryUpgradeHealth()
    {
        int cost = GetUpgradeCost(healthUpgradeLevel);
        if (permanentCurrency < cost) return false;

        permanentCurrency -= cost;
        healthUpgradeLevel++;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
            health.IncreaseMaxHealth(10f);

        SaveProgress();
        Debug.Log($"Health upgrade level {healthUpgradeLevel}", this);
        return true;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        return upgradeCostBase + currentLevel * 3;
    }

    private void LoadFromSave()
    {
        if (SaveDataManager.Instance == null) return;

        SaveData data = SaveDataManager.Instance.Data;
        permanentCurrency = data.permanentCurrency;
        attackUpgradeLevel = data.attackUpgradeLevel;
        healthUpgradeLevel = data.healthUpgradeLevel;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && healthUpgradeLevel > 0)
            health.IncreaseMaxHealth(healthUpgradeLevel * 10f);
    }

    private void SaveProgress()
    {
        if (SaveDataManager.Instance == null) return;

        SaveDataManager.Instance.SetPermanentProgress(
            permanentCurrency,
            attackUpgradeLevel,
            healthUpgradeLevel);
    }
}
