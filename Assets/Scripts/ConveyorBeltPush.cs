using UnityEngine;

public class ConveyorBeltPush : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float beltSpeed = 2f;
    
    [Header("Direction Settings")]
    [Tooltip("Kutularin gitmesini istediginiz yon.")]
    public Vector3 pushDirection = new Vector3(0, 0, -1);
    
    [Tooltip("True ise objenin kendi yonunu (Local), False ise dunya yonunu (Global) kullanir.")]
    public bool useLocalDirection = false;

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            if (pushDirection == Vector3.zero) return;

            Vector3 finalDirection = useLocalDirection ? transform.TransformDirection(pushDirection.normalized) : pushDirection.normalized;
            
            Vector3 targetVelocity = finalDirection * beltSpeed;
            Vector3 velocityChange = targetVelocity - rb.linearVelocity;
            
            velocityChange.y = 0;
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (pushDirection == Vector3.zero) return;
        Vector3 finalDirection = useLocalDirection ? transform.TransformDirection(pushDirection.normalized) : pushDirection.normalized;
        Vector3 start = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawRay(start, finalDirection * 1.5f);
        Gizmos.DrawSphere(start + finalDirection * 1.5f, 0.1f);
    }
}
