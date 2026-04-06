using UnityEngine;
using Unity.AI.Navigation;
using System.Collections;

/// <summary>
/// Manages runtime NavMesh updates when puddles spawn/despawn.
/// Attach this to the same GameObject as your NavMeshSurface(s), or assign them in the inspector.
/// For selective blocking (player blocked, enemies not), use TWO NavMeshSurfaces
/// with different Agent Types and set the NavMeshModifierVolume's affectedAgents accordingly.
/// </summary>
public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance { get; private set; }

    [Tooltip("All NavMeshSurfaces to update. If empty, auto-finds all in scene.")]
    [SerializeField] private NavMeshSurface[] surfaces;
    
    private bool isUpdatePending = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (surfaces == null || surfaces.Length == 0)
            surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// Requests a NavMesh rebake at end of frame. Multiple calls per frame are batched.
    /// </summary>
    public void RequestNavMeshUpdate()
    {
        if (!isUpdatePending && gameObject.activeInHierarchy)
            StartCoroutine(UpdateDelayed());
    }

    private IEnumerator UpdateDelayed()
    {
        isUpdatePending = true;
        // Wait one frame so multiple puddle spawns/destroys batch into a single rebake
        yield return new WaitForEndOfFrame();

        foreach (var surface in surfaces)
        {
            if (surface != null && surface.navMeshData != null)
            {
                surface.UpdateNavMesh(surface.navMeshData);
            }
        }

        isUpdatePending = false;
    }
}
