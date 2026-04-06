using UnityEngine;
using Newtonsoft.Json;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Metadata Metadata { get; private set; }
    public GameState GameState { get; private set; }

    private string SavePath => Application.persistentDataPath + "/gamestate.json";

    private void Awake()
    {
        Instance = this;
        LoadMetadata();
        LoadGameState();
    }

    private void LoadGameState()
    {
        if (System.IO.File.Exists(SavePath))
        {
            string json = System.IO.File.ReadAllText(SavePath);
            GameState = JsonConvert.DeserializeObject<GameState>(json);
            Debug.Log("Loaded existing GameState from save!");
        }
        else
        {
            GameState = new GameState();
            if (Metadata != null && Metadata.PlayerStats != null)
            {
                GameState.SyncFromMetadata(Metadata.PlayerStats, Metadata.Arsenal);
                Debug.Log("Initialized new GameState from Metadata!");
            }
        }
    }

    public void SaveGameState()
    {
        string json = JsonConvert.SerializeObject(GameState);
        System.IO.File.WriteAllText(SavePath, json);
        Debug.Log("Game State Saved!");
    }

    public void UpgradeStat(StatType type)
    {
        if (Metadata == null || Metadata.PlayerStats == null || GameState == null) return;
        var p = Metadata.PlayerStats;

        switch (type)
        {
            case StatType.Damage: GameState.CurrentDamage += p.DamageIncrement; break;
            case StatType.Range: GameState.LockOnRadius += p.RangeIncrement; break;
            case StatType.AttackInterval: GameState.AttackInterval = Mathf.Max(0.1f, GameState.AttackInterval - p.AttackIntervalDecrement); break;
            case StatType.Cooldown: GameState.Cooldown = Mathf.Max(0f, GameState.Cooldown - p.CooldownDecrement); break;
            case StatType.MoveSpeed: GameState.MoveSpeed += p.MoveSpeedIncrement; break;
        }
        SaveGameState();
        
        GameEvents.TriggerStatsUpdated();
    }

    private void LoadMetadata()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("Metadata");
        if (jsonText != null)
        {
            Metadata = JsonConvert.DeserializeObject<Metadata>(jsonText.text);
            Debug.Log("Successfully loaded Global Game Metadata natively via DataManager!");
        }
    }

    public EnemyData GetEnemyStats(EnemyType eType)
    {
        if (Metadata == null || Metadata.EnemyStats == null) return null;
        return Metadata.EnemyStats.Find(e => e.EnemyType == eType);
    }
}
