using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor tool that generates 48 geon-based 3D objects as prefabs,
/// following the stimulus design from Meijer & Van der Lubbe (2011).
///
/// Menu: Tools > Geon Objects > Generate All (48 objects)
/// </summary>
public class GeonObjectGenerator : EditorWindow
{
    private int seed = 42;
    private float mainBodyScale = 1.0f;
    private float attachmentScale = 0.55f;
    private int searchDirections = 32;
    private float flatAttachmentOverlap = 0.01f;
    private float curvedAttachmentOverlap = 0.12f;
    private Color geonColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    private string prefabFolder = "Assets/Prefabs/GeonObjects";
    private string meshFolder = "Assets/Meshes/Geons";
    private string materialFolder = "Assets/Materials";

    [MenuItem("Tools/Geon Objects/Generator Window")]
    static void ShowWindow()
    {
        GetWindow<GeonObjectGenerator>("Geon Object Generator");
    }

    [MenuItem("Tools/Geon Objects/Generate Meshes Only")]
    static void GenerateMeshesQuick()
    {
        var generator = new GeonObjectGenerator();
        generator.GenerateMeshes();
    }

    [MenuItem("Tools/Geon Objects/Generate All (48 objects)")]
    static void GenerateQuick()
    {
        var generator = new GeonObjectGenerator();
        generator.Generate();
    }

    void OnGUI()
    {
        GUILayout.Label("Geon Object Generator", EditorStyles.boldLabel);
        GUILayout.Label("Meijer & Van der Lubbe (2011) stimulus set", EditorStyles.miniLabel);
        GUILayout.Space(10);

        seed = EditorGUILayout.IntField("Random Seed", seed);
        mainBodyScale = EditorGUILayout.FloatField("Main Body Scale", mainBodyScale);
        attachmentScale = EditorGUILayout.FloatField("Attachment Scale", attachmentScale);
        searchDirections = EditorGUILayout.IntSlider(
            new GUIContent("Flat Search Directions", "Number of random directions raycast to find the flattest patch on each mesh (min 10)."),
            searchDirections, 10, 128);
        flatAttachmentOverlap = EditorGUILayout.Slider(
            new GUIContent("Flat Attachment", "Overlap used when the main body's contact patch is perfectly flat — a clean seam."),
            flatAttachmentOverlap, 0f, 0.2f);
        curvedAttachmentOverlap = EditorGUILayout.Slider(
            new GUIContent("Curved Attachment", "Overlap used when the main body's contact patch is fully curved — sinks the attachment in to hide the gap."),
            curvedAttachmentOverlap, 0f, 0.3f);
        geonColor = EditorGUILayout.ColorField("Geon Color (Gray)", geonColor);

        GUILayout.Space(10);
        prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);
        meshFolder = EditorGUILayout.TextField("Mesh Folder", meshFolder);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Meshes Only", GUILayout.Height(30)))
        {
            GenerateMeshes();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Generate 48 Objects", GUILayout.Height(40)))
        {
            Generate();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Geon Previews Only"))
        {
            GenerateGeonPreviews();
        }
    }

    /// <summary>
    /// Generates and saves the 24 geon mesh assets only (no prefabs).
    /// </summary>
    Mesh[] GenerateMeshes()
    {
        EnsureFolderPath(meshFolder);

        EditorUtility.DisplayProgressBar("Generating Geons", "Creating geon meshes...", 0f);
        Mesh[] geonMeshes = new Mesh[GeonMeshFactory.GeonCount];

        for (int i = 0; i < GeonMeshFactory.GeonCount; i++)
        {
            var type = (GeonMeshFactory.GeonType)i;
            geonMeshes[i] = GeonMeshFactory.Create(type);

            string meshPath = string.Format("{0}/{1}.asset", meshFolder, geonMeshes[i].name);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(geonMeshes[i], existing);
                geonMeshes[i] = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(geonMeshes[i], meshPath);
            }

            EditorUtility.DisplayProgressBar("Generating Geons",
                string.Format("Mesh {0}/{1}: {2}", i + 1, GeonMeshFactory.GeonCount, geonMeshes[i].name),
                (float)(i + 1) / GeonMeshFactory.GeonCount);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        Debug.Log(string.Format("[GeonObjectGenerator] Generated {0} geon meshes in {1}",
            GeonMeshFactory.GeonCount, meshFolder));

        return geonMeshes;
    }

    void Generate()
    {
        EnsureDirectories();

        // Step 1: Generate and save the 24 geon meshes
        Mesh[] geonMeshes = GenerateMeshes();

        // Step 2: Create shared material
        string matPath = materialFolder + "/GeonGray.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            material.color = geonColor;
            material.SetFloat("_Glossiness", 0.3f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, matPath);
        }
        else
        {
            material.color = geonColor;
        }

        // Step 3: Generate recipes
        EditorUtility.DisplayProgressBar("Generating Objects", "Computing object recipes...", 0.2f);
        var recipes = GeonObjectAssembler.GenerateRecipes(seed);

        // Step 4: Build and save prefabs
        for (int i = 0; i < recipes.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Generating Objects",
                string.Format("Building object {0}/{1}", i + 1, recipes.Length),
                0.2f + 0.8f * ((float)i / recipes.Length));

            GameObject obj = GeonObjectAssembler.BuildObject(recipes[i], geonMeshes, material,
                mainBodyScale, attachmentScale, seed, searchDirections, flatAttachmentOverlap, curvedAttachmentOverlap);

            string prefix = recipes[i].isComplex ? "Complex" : "Simple";
            int localId = recipes[i].isComplex ? recipes[i].objectId - 24 + 1 : recipes[i].objectId + 1;
            string prefabPath = string.Format("{0}/{1}_{2:D2}.prefab", prefabFolder, prefix, localId);

            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
            Object.DestroyImmediate(obj);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        // Log summary
        Debug.Log(string.Format(
            "[GeonObjectGenerator] Generated {0} objects ({1} simple + {2} complex) from {3} geon types.\n" +
            "Prefabs: {4}\nMeshes: {5}\nMaterial: {6}",
            recipes.Length, 24, 24, GeonMeshFactory.GeonCount,
            prefabFolder, meshFolder, matPath));

        LogUsageStats(recipes);
    }

    void GenerateGeonPreviews()
    {
        EnsureDirectories();

        string previewFolder = prefabFolder + "/GeonPreviews";
        if (!AssetDatabase.IsValidFolder(previewFolder))
        {
            AssetDatabase.CreateFolder(prefabFolder, "GeonPreviews");
        }

        string matPath = materialFolder + "/GeonGray.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            material.color = geonColor;
            material.SetFloat("_Glossiness", 0.3f);
            AssetDatabase.CreateAsset(material, matPath);
        }

        for (int i = 0; i < GeonMeshFactory.GeonCount; i++)
        {
            var type = (GeonMeshFactory.GeonType)i;
            Mesh mesh = GeonMeshFactory.Create(type);

            string meshPath = string.Format("{0}/{1}.asset", meshFolder, mesh.name);
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }
            else
            {
                mesh = existingMesh;
            }

            var go = new GameObject(string.Format("Geon_{0:D2}_{1}", i, GeonMeshFactory.GetGeonName(type)));
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;

            string path = string.Format("{0}/Geon_{1:D2}_{2}.prefab", previewFolder, i, GeonMeshFactory.GetGeonName(type));
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(string.Format("[GeonObjectGenerator] Generated {0} geon preview prefabs in {1}",
            GeonMeshFactory.GeonCount, previewFolder));
    }

    void EnsureDirectories()
    {
        EnsureFolderPath(prefabFolder);
        EnsureFolderPath(meshFolder);
        EnsureFolderPath(materialFolder);
    }

    static void EnsureFolderPath(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    static void LogUsageStats(GeonObjectAssembler.ObjectRecipe[] recipes)
    {
        int[] counts = new int[GeonMeshFactory.GeonCount];
        int[] mainCounts = new int[GeonMeshFactory.GeonCount];

        foreach (var r in recipes)
        {
            counts[r.mainBodyGeon]++;
            mainCounts[r.mainBodyGeon]++;
            foreach (int g in r.attachmentGeons)
            {
                counts[g]++;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[GeonObjectGenerator] Usage statistics:");
        sb.AppendLine("Geon | Total Uses | As Main Body");
        sb.AppendLine("-----|------------|-------------");
        for (int i = 0; i < GeonMeshFactory.GeonCount; i++)
        {
            sb.AppendFormat("{0,4} | {1,10} | {2,12}\n",
                GeonMeshFactory.GetGeonName((GeonMeshFactory.GeonType)i),
                counts[i], mainCounts[i]);
        }
        Debug.Log(sb.ToString());
    }
}
