using UnityEngine;

/// <summary>
/// Übernimmt nur die Rotation eines Ziel-Transforms (z. B. des Vive-Controllers),
/// behält dabei aber die eigene Position bei.
/// </summary>
public class FollowControllerRotation : MonoBehaviour
{
    [Tooltip("Transform, dessen Rotation übernommen werden soll (z. B. der Controller).")]
    [SerializeField] private Transform rotationSource;

    [Tooltip("Zusätzlicher, fest eingestellter Rotations-Offset in Grad (X, Y, Z).")]
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;

    private Vector3 fixedPosition;

    private void Awake()
    {
        // Aktuelle Position merken – wird danach nicht mehr verändert.
        fixedPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (rotationSource != null)
        {
            transform.rotation = rotationSource.rotation * Quaternion.Euler(rotationOffsetEuler);
        }

        // Position konstant halten, unabhängig davon, wie sich Controller/Eltern bewegen.
        transform.position = fixedPosition;
    }
}