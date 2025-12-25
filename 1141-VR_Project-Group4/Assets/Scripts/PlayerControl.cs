using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CharacterController))]
public class SimpleCCPlayer_Restored : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float runSpeed = 7.5f;

    [Header("Input")]
    [Tooltip("仅使用键盘输入(WSAD)，避免旧输入系统将手柄/VR控制器轴混入导致漂移")] public bool keyboardOnlyInput = true;
    [Tooltip("对模拟轴输入应用死区，解决手柄或控制器轻微漂移")] public float axisDeadzone = 0.2f;
    [Tooltip("水平轴名称(旧输入系统)")] public string horizontalAxis = "Horizontal";
    [Tooltip("垂直轴名称(旧输入系统)")] public string verticalAxis = "Vertical";

    [Header("Mouse Look")]
    public float mouseSensitivity = 1.2f;
    public bool invertY = false;
    public float pitchClamp = 85f;

    [Header("Gravity")]
    [Tooltip("重力加速度 (米/秒^2)，为负值向下")] public float gravity = -9.81f;
    [Tooltip("接地时维持的轻微向下速度，保证贴地")] public float groundedGravity = -2f;

    [Header("References")]
    [Tooltip("玩家相机(建议为玩家物体子节点)。若为空，将尝试 Camera.main")] public Camera playerCamera;

    [Header("Footsteps")]
    [Tooltip("是否启用脚步音效")] public bool enableFootsteps = true;
    [Tooltip("脚步音源。若为空，会自动创建并挂在玩家上")] public AudioSource footstepSource;
    [Tooltip("脚步音频片段集合，随机播放其中之一")] public AudioClip[] footstepClips;
    [Tooltip("启动时自动从 Resources 加载单个脚步音 clip（用于无需手动指定的场景）")] public bool autoLoadFromResources = true;
    [Tooltip("Resources 下的路径（不含扩展名），例如 'sound/walk' 表示 Assets/Resources/sound/walk.wav")] public string resourcesClipPath = "sound/walk";
    [Tooltip("行走每秒脚步次数（步频）")] public float walkStepRate = 1.8f;
    [Tooltip("奔跑每秒脚步次数（步频）")] public float runStepRate = 2.6f;
    [Range(0f, 1f)] public float footstepVolume = 0.9f;
    [Tooltip("触发脚步音效所需的最小水平速度")] public float minVelocityToStep = 0.1f;
    [Tooltip("脚步音为3D（1）或2D（0）。建议3D用于VR/第一人称")] [Range(0f,1f)] public float spatialBlend = 1f;

    CharacterController controller;
    float yaw;
    float pitch;
    float verticalVelocity;
    float stepTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.08f;
        controller.minMoveDistance = 0.001f;

        if (playerCamera == null)
        {
            if (Camera.main != null) playerCamera = Camera.main;
            else playerCamera = GetComponentInChildren<Camera>();
        }

        if (enableFootsteps && footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = spatialBlend;
            footstepSource.volume = footstepVolume;
        }

        yaw = transform.eulerAngles.y;
        if (playerCamera != null)
        {
            pitch = NormalizeAngle(playerCamera.transform.localEulerAngles.x);
        }

        TryAutoLoadFootstepClip();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
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

        Vector2 moveInput = GetMoveInput();
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        input = Vector3.ClampMagnitude(input, 1f);

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : moveSpeed;
        Vector3 moveXZ = (transform.right * input.x + transform.forward * input.z) * speed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveXZ + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (enableFootsteps)
        {
            HandleFootsteps(moveXZ, speed);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 可按需调试碰撞：
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

    Vector2 GetMoveInput()
    {
        if (keyboardOnlyInput)
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.W)) y += 1f;
            return new Vector2(x, y);
        }
        else
        {
            float ix = Input.GetAxisRaw(horizontalAxis);
            float iz = Input.GetAxisRaw(verticalAxis);
            ix = ApplyDeadzone(ix, axisDeadzone);
            iz = ApplyDeadzone(iz, axisDeadzone);
            return new Vector2(ix, iz);
        }
    }

    float ApplyDeadzone(float v, float dz)
    {
        return Mathf.Abs(v) < dz ? 0f : v;
    }

    void HandleFootsteps(Vector3 moveXZ, float speed)
    {
        float horizSpeed = new Vector3(moveXZ.x, 0f, moveXZ.z).magnitude;
        bool moving = horizSpeed >= minVelocityToStep - 1e-3f;
        if (!controller.isGrounded || !moving || footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
        {
            stepTimer = 0f;
            return;
        }
        float rate = (Mathf.Approximately(speed, runSpeed) ? runStepRate : walkStepRate);
        float interval = rate > 0f ? (1f / rate) : 0.5f;
        stepTimer += Time.deltaTime;
        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            PlayRandomFootstep();
        }
    }

    void PlayRandomFootstep()
    {
        if (footstepSource == null) return;
        AudioClip clip = null;
        if (footstepClips != null && footstepClips.Length > 0)
        {
            int idx = Random.Range(0, footstepClips.Length);
            clip = footstepClips[idx];
        }
        if (clip == null) return;
        footstepSource.volume = footstepVolume;
        footstepSource.spatialBlend = spatialBlend;
        footstepSource.PlayOneShot(clip);
    }

    void TryAutoLoadFootstepClip()
    {
        if (!enableFootsteps) return;
        bool hasClips = footstepClips != null && footstepClips.Length > 0 && footstepClips[0] != null;
        if (hasClips) return;
        if (autoLoadFromResources && !string.IsNullOrEmpty(resourcesClipPath))
        {
            var resClip = Resources.Load<AudioClip>(resourcesClipPath);
            if (resClip != null)
            {
                footstepClips = new AudioClip[] { resClip };
                return;
            }
        }
#if UNITY_EDITOR
        string editorPath = "Assets/sound/walk.wav";
        var editorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(editorPath);
        if (editorClip != null)
        {
            footstepClips = new AudioClip[] { editorClip };
        }
#endif
    }
}
