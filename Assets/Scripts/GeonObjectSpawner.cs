using UnityEngine;

/// <summary>
/// Runtime utility to instantiate geon object prefabs.
/// Assign prefabs in the inspector or load from a folder.
/// </summary>
public class GeonObjectSpawner : MonoBehaviour
{
    [Header("Prefab References")]
    [Tooltip("Assign all 24 simple object prefabs (Simple_01 through Simple_24)")]
    public GameObject[] simplePrefabs = new GameObject[0];

    [Tooltip("Assign all 24 complex object prefabs (Complex_01 through Complex_24)")]
    public GameObject[] complexPrefabs = new GameObject[0];

    [Header("Spawn Settings")]
    public Transform spawnPoint;
    public float spawnScale = 1f;

    /// <summary>
    /// Spawns a simple object by index (1-24).
    /// </summary>
    public GameObject SpawnSimple(int id)
    {
        if (id < 1 || id > simplePrefabs.Length)
        {
            Debug.LogError(string.Format("SpawnSimple: id {0} out of range [1..{1}]", id, simplePrefabs.Length));
            return null;
        }
        return Instantiate(id - 1, simplePrefabs);
    }

    /// <summary>
    /// Spawns a complex object by index (1-24).
    /// </summary>
    public GameObject SpawnComplex(int id)
    {
        if (id < 1 || id > complexPrefabs.Length)
        {
            Debug.LogError(string.Format("SpawnComplex: id {0} out of range [1..{1}]", id, complexPrefabs.Length));
            return null;
        }
        return Instantiate(id - 1, complexPrefabs);
    }

    /// <summary>
    /// Spawns an object by its global ID (0-47).
    /// 0-23 = simple, 24-47 = complex.
    /// </summary>
    public GameObject SpawnByGlobalId(int globalId)
    {
        if (globalId < 0 || globalId >= 48)
        {
            Debug.LogError(string.Format("SpawnByGlobalId: id {0} out of range [0..47]", globalId));
            return null;
        }

        if (globalId < 24)
            return Instantiate(globalId, simplePrefabs);
        else
            return Instantiate(globalId - 24, complexPrefabs);
    }

    /// <summary>
    /// Removes all spawned objects that are children of the spawn point.
    /// </summary>
    public void ClearSpawned()
    {
        Transform parent = spawnPoint != null ? spawnPoint : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    GameObject Instantiate(int index, GameObject[] prefabs)
    {
        if (prefabs == null || index < 0 || index >= prefabs.Length || prefabs[index] == null)
        {
            Debug.LogError(string.Format("GeonObjectSpawner: prefab at index {0} is missing.", index));
            return null;
        }

        Transform parent = spawnPoint != null ? spawnPoint : transform;
        Vector3 pos = parent.position;
        Quaternion rot = parent.rotation;

        GameObject instance = Object.Instantiate(prefabs[index], pos, rot, parent);
        instance.transform.localScale = Vector3.one * spawnScale;
        return instance;
    }
}
