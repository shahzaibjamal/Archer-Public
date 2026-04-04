using UnityEngine;
using Newtonsoft.Json;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    public Metadata Metadata { get; private set; }

    private void Awake()
    {
        Instance = this;
        LoadMetadata();
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

    public EnemyData GetEnemyStats(string eType)
    {
        if (Metadata == null || Metadata.EnemyStats == null) return null;
        return Metadata.EnemyStats.Find(e => e.EnemyType == eType);
    }
}
