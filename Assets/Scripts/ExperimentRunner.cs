using System.Collections;
using UnityEngine;

/// <summary>
/// Cycles through a pool of prefabs at runtime. Each round picks a random prefab
/// (never the same one twice in a row), instantiates it as a child of this
/// transform for <see cref="displayDuration"/> seconds, destroys it, waits
/// <see cref="pauseDuration"/> seconds, then proceeds to the next. Pressing Space
/// or clicking the mouse skips to the next object early. After
/// <see cref="iterations"/> rounds the runner waits for a Space press or mouse
/// click, then restarts the cycle.
/// </summary>
public class ExperimentRunner : MonoBehaviour
{
    [Header("Prefab Pool")]
    [Tooltip("Prefabs to randomly choose from. Use the context menu " +
             "\"Auto-Fill Prefabs From Folder\" to populate from Assets/Prefabs/GeonObjects.")]
    public GameObject[] prefabs = new GameObject[0];

    [Header("Timing")]
    [Tooltip("Seconds an instantiated prefab stays in the scene.")]
    public float displayDuration = 30f;

    [Tooltip("Seconds between destroying one prefab and instantiating the next.")]
    public float pauseDuration = 1f;

    [Tooltip("How many prefabs to show in total before stopping.")]
    public int iterations = 10;

    void Start()
    {
        // Hide this host object's own renderer(s) — it acts as a spawn anchor,
        // not something that should be visible during the showcase.
        foreach (var rend in GetComponents<Renderer>())
            rend.enabled = false;

        StartCoroutine(CycleRoutine());
    }

    IEnumerator CycleRoutine()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("[ExperimentRunner] No prefabs assigned.", this);
            yield break;
        }

        int lastIndex = -1;
        while (true)
        {
            var shown = new System.Collections.Generic.List<int>();
            for (int i = 0; i < iterations; i++)
            {
                int idx = PickNextIndex(shown, lastIndex);
                lastIndex = idx;
                shown.Add(idx);

                GameObject prefab = prefabs[idx];
                GameObject instance = null;
                if (prefab != null)
                {
                    instance = Instantiate(prefab, transform);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogWarning(string.Format("[ExperimentRunner] Prefab at index {0} is null.", idx), this);
                }

                // Show until the time elapses or the user skips with Space / click.
                yield return WaitForDurationOrSkip(displayDuration);

                if (instance != null) Destroy(instance);

                // Skip the trailing pause after the final round so the cycle ends cleanly.
                if (i < iterations - 1)
                    yield return new WaitForSeconds(pauseDuration);
            }

            // All iterations done — hold until the user presses Space or clicks, then restart.
            yield return WaitForSkip();
        }
    }

    static bool SkipPressed()
    {
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonUp(0);
    }

    IEnumerator WaitForDurationOrSkip(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (SkipPressed()) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator WaitForSkip()
    {
        // Consume the current frame so the keypress/click that ended the last
        // object isn't immediately re-read as the restart trigger.
        yield return null;
        while (!SkipPressed())
            yield return null;
    }

    int PickNextIndex(System.Collections.Generic.List<int> shown, int lastIndex)
    {
        // Candidate pool = prefabs not shown yet and not the immediately previous
        // one (the latter only matters at the first pick of a restarted cycle,
        // where `shown` is empty but `lastIndex` carries over).
        var pool = new System.Collections.Generic.List<int>();
        for (int i = 0; i < prefabs.Length; i++)
            if (!shown.Contains(i) && i != lastIndex) pool.Add(i);

        // Once every prefab has been shown, reset the history so the cycle can
        // keep going (iterations may exceed the prefab count). Avoid repeating
        // the just-shown prefab across the reset boundary.
        if (pool.Count == 0)
        {
            shown.Clear();
            for (int i = 0; i < prefabs.Length; i++)
                if (i != lastIndex || prefabs.Length == 1) pool.Add(i);
        }

        return pool[Random.Range(0, pool.Count)];
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Fill Prefabs From Folder")]
    void AutoFillPrefabs()
    {
        const string folder = "Assets/Prefabs/GeonObjects";
        var guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            // Only direct children of the folder — skip GeonPreviews and other subfolders.
            string dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (dir != folder) continue;
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) list.Add(go);
        }
        prefabs = list.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log(string.Format("[ExperimentRunner] Loaded {0} prefabs from {1}.", list.Count, folder));
    }
#endif
}
