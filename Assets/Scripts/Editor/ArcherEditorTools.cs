using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class ArcherEditorTools : EditorWindow
{
    [MenuItem("Tools/Clear GameState")]
    public static void ClearGameState()
    {
        string path = Application.persistentDataPath + "/gamestate.json";
        if (File.Exists(path))
        {
            File.Delete(path);
            UnityEngine.Debug.Log("[Archer] GameState cleared successfully at: " + path);
        }
        else
        {
            UnityEngine.Debug.Log("[Archer] No GameState file found to delete.");
        }
    }

    [MenuItem("Tools/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        string path = Application.persistentDataPath;
        if (Directory.Exists(path))
        {
            Process.Start("explorer.exe", path.Replace("/", "\\"));
        }
        else
        {
            UnityEngine.Debug.LogError("[Archer] Persistent Data Path does not exist: " + path);
        }
    }
}
