using UnityEngine;

/// <summary>
/// Constrains this GameObject's position to stay within a sphere centered on the
/// position it had when the scene started. The position is treated relative to
/// the parent (via <c>localPosition</c>); when there is no parent, Unity's
/// <c>localPosition</c> already equals the world position, so the same code
/// covers both cases. A maxDistance of 0 locks the object to its initial position.
/// </summary>
public class PositionLimit : MonoBehaviour
{
    [Tooltip("Maximum distance (in parent/local space) the object may move from " +
             "its initial position. 0 locks it in place.")]
    [Min(0f)]
    public float maxDistance = 0f;

    Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        float maxR = Mathf.Max(0f, maxDistance);
        Vector3 offset = transform.localPosition - initialPosition;
        if (offset.sqrMagnitude > maxR * maxR)
            transform.localPosition = initialPosition + offset.normalized * maxR;
    }
}
