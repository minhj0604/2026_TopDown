using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int bestDungeonLevel = 1;
    public int bestNodeIndex = 0;
    public int permanentCurrency = 0;
    public int attackUpgradeLevel = 0;
    public int healthUpgradeLevel = 0;
    public bool tutorialCompleted = false;
    public List<string> unlockedWeapons = new List<string>();
}

public class SaveDataManager : MonoBehaviour
{
    public static SaveDataManager Instance { get; private set; }

    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool showDebugUI = true;

    public SaveData Data { get; private set; } = new SaveData();
    public string SavePath => Path.Combine(Application.persistentDataPath, "save_data.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadOnAwake)
            Load();
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Saved JSON data: {SavePath}", this);
    }

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Data = new SaveData();
            return;
        }

        string json = File.ReadAllText(SavePath);
        Data = JsonUtility.FromJson<SaveData>(json);
        if (Data == null)
            Data = new SaveData();
    }

    public void RecordDungeonProgress(int dungeonLevel, int nodeIndex)
    {
        if (dungeonLevel > Data.bestDungeonLevel)
        {
            Data.bestDungeonLevel = dungeonLevel;
            Data.bestNodeIndex = nodeIndex;
            Save();
            return;
        }

        if (dungeonLevel == Data.bestDungeonLevel && nodeIndex > Data.bestNodeIndex)
        {
            Data.bestNodeIndex = nodeIndex;
            Save();
        }
    }

    public void AddCurrency(int amount)
    {
        Data.permanentCurrency = Mathf.Max(0, Data.permanentCurrency + amount);
        Save();
    }

    public void SetPermanentProgress(int currency, int attackLevel, int healthLevel)
    {
        Data.permanentCurrency = Mathf.Max(0, currency);
        Data.attackUpgradeLevel = Mathf.Max(0, attackLevel);
        Data.healthUpgradeLevel = Mathf.Max(0, healthLevel);
        Save();
    }

    public void SetTutorialCompleted(bool completed)
    {
        Data.tutorialCompleted = completed;
    }

}
