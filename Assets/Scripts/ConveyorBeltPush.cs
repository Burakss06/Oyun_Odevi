using UnityEngine;

/// <summary>
/// Conveyor belt that pushes objects using surface contact detection.
/// Attach to belt mesh with BoxCollider. Objects with Rigidbody touching
/// the belt will be continuously pushed in worldPushDirection.
/// </summary>
public class ConveyorBeltPush : MonoBehaviour
{
    [Header("Conveyor Settings")]
    [Tooltip("Speed of the conveyor belt in m/s")]
    public float speed = 2.0f;

    [Tooltip("Push direction in WORLD space")]
    public Vector3 worldPushDirection = new Vector3(0, 0, -1);

    void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null || rb.isKinematic) return;

        // Unlock constraints
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Use MovePosition for reliable movement that respects collisions
        Vector3 dir = worldPushDirection.normalized;
        Vector3 newPos = rb.position + dir * speed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }
}
