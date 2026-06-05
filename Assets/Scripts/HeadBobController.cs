using UnityEngine;
using UnityEngine.InputSystem;

public class HeadBobController : MonoBehaviour
{
    [Header("Bob Settings - Walk")]
    [SerializeField] private float walkBobSpeed = 10f;
    [SerializeField] private float walkBobAmountY = 0.035f;
    [SerializeField] private float walkBobAmountX = 0.015f;

    [Header("Bob Settings - Run")]
    [SerializeField] private float runBobSpeed = 14f;
    [SerializeField] private float runBobAmountY = 0.06f;
    [SerializeField] private float runBobAmountX = 0.03f;

    [Header("Tilt Settings")]
    [SerializeField] private float walkTiltAmount = 0.5f;
    [SerializeField] private float runTiltAmount = 1.2f;

    [Header("Smooth Settings")]
    [SerializeField] private float bobSmoothing = 10f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private float bobTimer = 0f;
    private Vector3 originalLocalPos;
    private float targetTilt = 0f;
    private float currentTilt = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform != null)
        {
            originalLocalPos = cameraTransform.localPosition;
        }
    }

    void Update()
    {
        if (cameraTransform == null || controller == null) return;

        // Don't bob if game is not in playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            ResetBob();
            return;
        }

        // Check if player is moving and grounded
        bool isGrounded = controller.isGrounded;
        Vector2 input = GetMovementInput();
        bool isMoving = input.magnitude > 0.1f;
        bool isRunning = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        if (isGrounded && isMoving)
        {
            // Select bob parameters based on walk/run
            float bobSpeed = isRunning ? runBobSpeed : walkBobSpeed;
            float bobAmountY = isRunning ? runBobAmountY : walkBobAmountY;
            float bobAmountX = isRunning ? runBobAmountX : walkBobAmountX;
            float tiltAmount = isRunning ? runTiltAmount : walkTiltAmount;

            // Advance bob timer
            bobTimer += Time.deltaTime * bobSpeed;

            // Calculate bob offset using sine waves
            float bobOffsetY = Mathf.Sin(bobTimer) * bobAmountY;
            float bobOffsetX = Mathf.Sin(bobTimer * 0.5f) * bobAmountX;

            // Calculate tilt (lean) based on sideways movement
            targetTilt = -input.x * tiltAmount;

            // Apply bob to camera local position
            Vector3 targetPos = originalLocalPos + new Vector3(bobOffsetX, bobOffsetY, 0f);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            // Smoothly return to original position when not moving
            bobTimer = 0f;
            targetTilt = 0f;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalLocalPos, Time.deltaTime * bobSmoothing);
        }

        // Apply tilt rotation smoothly
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * bobSmoothing);
        
        // Get current camera rotation and add tilt on Z axis
        Vector3 currentEuler = cameraTransform.localEulerAngles;
        cameraTransform.localRotation = Quaternion.Euler(currentEuler.x, currentEuler.y, currentTilt);
    }

    private Vector2 GetMovementInput()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
        }
        return input;
    }

    private void ResetBob()
    {
        bobTimer = 0f;
        targetTilt = 0f;
        currentTilt = 0f;
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalLocalPos;
        }
    }
}
