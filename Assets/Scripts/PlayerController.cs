using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5.0f;
    [SerializeField] private float runSpeed = 8.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float lookUpLimit = -90f;
    [SerializeField] private float lookDownLimit = 90f;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Başlangıç durumuna göre imleci ayarla
        UpdateCursorState();
    }

    void Update()
    {
        // Eğer oyun oynanış modunda değilse (Menü, Briefing veya Rapor açıkken)
        // oyuncu hareketini ve kamerayı kilitle, imleci serbest bırak.
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        // Oyun oynanış modundaysa ve imleç yanlışlıkla serbest kaldıysa kilitle
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Harita dışına düşme kontrolü (Y < -10f ise başlangıç noktasına ışınla)
        if (transform.position.y < -10f)
        {
            ResetToStartPosition();
        }

        HandleMovement();
        HandleLook();
    }

    private void UpdateCursorState()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMovement()
    {
        // Grounded check
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
        }

        float currentSpeed = walkSpeed;
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            currentSpeed = runSpeed;
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
        }

        // Adjusting sensitivity for New Input System's pixel delta
        float mouseX = mouseDelta.x * mouseSensitivity * 0.05f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.05f;

        // Vertical look (Camera)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, lookUpLimit, lookDownLimit);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Horizontal look (Player Body)
        transform.Rotate(Vector3.up * mouseX);
    }

    public void ResetToStartPosition()
    {
        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
        
        xRotation = 0f;
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.identity;
        }

        velocity = Vector3.zero;

        if (controller != null)
        {
            controller.enabled = true;
        }

        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null)
        {
            interaction.ResetInteraction();
        }
    }
}
