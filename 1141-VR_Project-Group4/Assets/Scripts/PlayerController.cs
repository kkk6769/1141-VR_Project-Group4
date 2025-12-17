using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintMultiplier = 1.8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform; // 请将玩家的相机（一般是子物体）拖到这里
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Developer Fly Mode")]
    public KeyCode toggleFlyKey = KeyCode.F;
    public float flySpeedMultiplier = 1.2f;
    public KeyCode ascendKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.LeftControl; // 也可改成 KeyCode.C

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation; // 相机俯仰角（Pitch）
    private bool isFlyMode;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleToggleFlyMode();
        HandleMouseLook();
        if (isFlyMode)
        {
            HandleFlyMovement();
        }
        else
        {
            HandleMovement();
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 水平旋转玩家（Yaw）
        transform.Rotate(Vector3.up * mouseX);

        // 垂直旋转相机（Pitch）并限制角度
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, pitchMin, pitchMax);
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.sqrMagnitude > 1f) move.Normalize(); // 斜向不超速

        float speed = moveSpeed * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f);
        Vector3 velocity = move * speed;

        // 简单重力
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f; // 保持贴地
        }
        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFlyMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        float y = 0f;
        if (Input.GetKey(ascendKey)) y += 1f;
        if (Input.GetKey(descendKey)) y -= 1f;

        Vector3 move = (transform.right * x) + (transform.forward * z) + (Vector3.up * y);
        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = moveSpeed * flySpeedMultiplier * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f);
        transform.position += move * speed * Time.deltaTime;
    }

    void HandleToggleFlyMode()
    {
        if (Input.GetKeyDown(toggleFlyKey))
        {
            isFlyMode = !isFlyMode;
            if (controller)
            {
                controller.enabled = !isFlyMode;
            }
            if (!isFlyMode)
            {
                verticalVelocity = 0f; // 退出飞行时重置落体速度
            }
        }
    }

    // 可选：外部切换鼠标锁定状态
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
