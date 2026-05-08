using UnityEngine;

public class MainConveyorBelt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 2.5f;
    
    [Header("Direction Settings")]
    [Tooltip("Hangi yone itsin?")]
    public Vector3 pushDirection = new Vector3(-1f, 0f, 0f);
    
    [Tooltip("True ise objenin kendi yonunu (Local), False ise dunya yonunu (Global) kullanir.")]
    public bool useLocalDirection = false;

    private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 finalDirection = useLocalDirection ? transform.TransformDirection(pushDirection.normalized) : pushDirection.normalized;
            
            Vector3 targetVelocity = finalDirection * speed;
            Vector3 velocityChange = targetVelocity - rb.linearVelocity;
            
            velocityChange.y = 0;
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (pushDirection == Vector3.zero) return;
        Vector3 finalDirection = useLocalDirection ? transform.TransformDirection(pushDirection.normalized) : pushDirection.normalized;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.6f, finalDirection * 2f);
    }
}
