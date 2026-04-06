using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoader : MonoBehaviour
{
    public static AssetLoader Instance { get; private set; }

    private Dictionary<string, AsyncOperationHandle<GameObject>> _handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async System.Threading.Tasks.Task<GameObject> LoadAsset(string key)
    {
        if (_handles.ContainsKey(key))
        {
            return _handles[key].Result;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
        _handles[key] = handle;

        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            return handle.Result;
        }
        else
        {
            _handles.Remove(key);
            Debug.LogError($"[AssetLoader] Failed to load move Addressable: {key}");
            return null;
        }
    }

    public void ReleaseAsset(string key)
    {
        if (_handles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(key);
        }
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;
        Addressables.ReleaseInstance(instance);
    }

    private void OnDestroy()
    {
        foreach (var handle in _handles.Values)
        {
            Addressables.Release(handle);
        }
        _handles.Clear();
    }
}
