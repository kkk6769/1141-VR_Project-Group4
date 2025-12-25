using UnityEngine;

// 独立开发者模式脚本：飞行/穿透与滚轮调速。
// 用法：挂到玩家对象；在 Inspector 勾选 developerModeEnabled 开启。
// - F 切换飞行（保留碰撞、不受重力）
// - R 切换穿透（禁用 CharacterController，直接移动 transform）
// - 鼠标滚轮 调整速度（带最小/最大与缩放系数）
// 在开发者模式激活时，会暂时禁用 SimpleCCPlayer/CharacterController 以避免冲突。
[DisallowMultipleComponent]
public class GodMode : MonoBehaviour
{
    [Header("Developer Mode")]
    public bool developerModeEnabled = true;
    public KeyCode toggleFlyKey = KeyCode.F;
    public KeyCode toggleNoclipKey = KeyCode.R;
    public KeyCode ascendKey = KeyCode.Space;
    public KeyCode descendKey = KeyCode.LeftControl;

    [Header("Mouse Look (Dev Mode)")]
    [Tooltip("开发者模式下的鼠标灵敏度")] public float mouseSensitivity = 1.2f;
    [Tooltip("开发者模式下是否反转Y轴")] public bool invertY = false;
    [Tooltip("俯仰角限制")] public float pitchClamp = 85f;
    [Tooltip("玩家相机。若为空，将尝试 Camera.main 或子物体相机")] public Camera playerCamera;

    [Header("Speed")]
    public float flySpeed = 6f;
    public float flySpeedMin = 1f;
    public float flySpeedMax = 20f;
    public float flyScrollScale = 50f; // 提高滚轮调速倍率

    CharacterController controller;
    Behaviour playerControllerScript; // 兼容 SimpleCCPlayer 或其它同类控制脚本
    bool isFlying;
    bool isNoclip;
    float yaw;
    float pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        // 尝试获取玩家移动脚本（这里假设名为 SimpleCCPlayer 或 SimpleCCPlayer_Restored）
        var simple = GetComponent("SimpleCCPlayer") as Behaviour;
        if (simple == null)
        {
            simple = GetComponent("SimpleCCPlayer_Restored") as Behaviour;
        }
        playerControllerScript = simple;

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
    }

    void Update()
    {
        if (!developerModeEnabled)
        {
            RestoreStates();
            return;
        }

        // 切换模式
        if (Input.GetKeyDown(toggleFlyKey))
        {
            isFlying = !isFlying;
            if (isFlying) { EnsurePlayerDisabled(); }
        }
        if (Input.GetKeyDown(toggleNoclipKey))
        {
            isNoclip = !isNoclip;
            if (controller != null) controller.enabled = !isNoclip;
            EnsurePlayerDisabled();
        }

        // 滚轮调速
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            flySpeed = Mathf.Clamp(flySpeed + scroll * flyScrollScale, flySpeedMin, flySpeedMax);
        }

        bool usingDevMove = isFlying || isNoclip;
        if (!usingDevMove)
        {
            // 没有激活任何开发者移动，恢复玩家控制
            RestorePlayerEnabled();
            return;
        }

        // 开发者模式下鼠标视角（禁用玩家控制时由此接管）
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

        // 方向输入（键盘 WASD + 上下）
        float x = 0f, z = 0f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) z -= 1f;
        if (Input.GetKey(KeyCode.W)) z += 1f;
        float upDown = 0f;
        if (Input.GetKey(ascendKey)) upDown += 1f;
        if (Input.GetKey(descendKey)) upDown -= 1f;

        Vector3 dir = transform.right * x + transform.forward * z + Vector3.up * upDown;
        dir = Vector3.ClampMagnitude(dir, 1f);
        Vector3 freeVel = dir * flySpeed;

        if (isNoclip)
        {
            // 穿透：直接位移，不做碰撞
            transform.position += freeVel * Time.deltaTime;
        }
        else
        {
            // 飞行：保持 CharacterController 碰撞，不受重力
            if (controller != null) controller.Move(freeVel * Time.deltaTime);
            else transform.position += freeVel * Time.deltaTime;
        }
    }

    void OnDisable()
    {
        RestoreStates();
    }

    void EnsurePlayerDisabled()
    {
        if (playerControllerScript != null) playerControllerScript.enabled = false;
    }

    void RestorePlayerEnabled()
    {
        if (playerControllerScript != null) playerControllerScript.enabled = true;
    }

    void RestoreStates()
    {
        isFlying = false;
        isNoclip = false;
        if (controller != null) controller.enabled = true;
        if (playerControllerScript != null) playerControllerScript.enabled = true;
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
