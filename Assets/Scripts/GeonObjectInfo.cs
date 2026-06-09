using UnityEngine;

/// <summary>
/// Metadata component attached to each generated geon object prefab.
/// Stores the object's ID, complexity level, and which geon types were used.
/// </summary>
public class GeonObjectInfo : MonoBehaviour
{
    public enum Complexity { Simple, Complex }

    [Header("Object Identity")]
    public int objectId;
    public Complexity complexity;

    [Header("Geon Composition")]
    [Tooltip("Index of the geon used as the main body")]
    public int mainBodyGeonIndex;

    [Tooltip("Indices of geons used as attachments")]
    public int[] attachmentGeonIndices;

    [Header("Labels")]
    public string objectName;
    public string[] geonNames;

    /// <summary>
    /// Total number of components (main body + attachments).
    /// Simple = 3, Complex = 5.
    /// </summary>
    public int ComponentCount
    {
        get { return 1 + (attachmentGeonIndices != null ? attachmentGeonIndices.Length : 0); }
    }
}
