using UnityEngine;

// 一个“最简单、稳定无回弹”的玩家控制器：
// - 使用 CharacterController 进行碰撞（非刚体），几乎没有物理回弹感
// - 支持 WASD 移动、Left Shift 奔跑、鼠标转向（相机俯仰）
// - 自带重力，默认不含跳跃（需要可扩展）
[RequireComponent(typeof(CharacterController))]
public class SimpleCCPlayer : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float runSpeed = 7.5f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 1.2f;
    public bool invertY = false;
    public float pitchClamp = 85f;

    [Header("Gravity")]
    [Tooltip("重力加速度 (米/秒^2)，为负值向下")] public float gravity = -9.81f;
    [Tooltip("接地时维持的轻微向下速度，保证贴地")] public float groundedGravity = -2f;

    [Header("References")]
    [Tooltip("玩家相机(建议为玩家物体子节点)。若为空，将尝试 Camera.main")] public Camera playerCamera;

    CharacterController controller;
    float yaw;
    float pitch;
    float verticalVelocity; // y 方向速度（重力）

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // 尽量简洁稳定的 CharacterController 参数（可按需要在 Inspector 中调整）
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0.001f;

        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
            else playerCamera = GetComponentInChildren<Camera>();
        }

        yaw = transform.eulerAngles.y;
        if (playerCamera != null)
        {
            pitch = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 鼠标视角
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        float yInput = invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch + yInput, -pitchClamp, pitchClamp);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // 移动输入（本地XZ）
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(ix, 0f, iz);
        input = Vector3.ClampMagnitude(input, 1f);

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        Vector3 moveXZ = (transform.right * input.x + transform.forward * input.z) * speed;

        // 重力与贴地
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = groundedGravity; // 略微向下，避免“悬空”
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveXZ + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 这里说明：CharacterController 与任意带 Collider 的物体发生接触，不会产生物理反弹
        // 可按需调试：
        // Debug.Log($"Hit: {hit.collider.name}");
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
