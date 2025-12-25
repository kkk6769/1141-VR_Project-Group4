using UnityEngine;

[DisallowMultipleComponent]
public class Door : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 4f;
    public Transform pivot;
    public bool autoSetupPhysics = true;
    public Collider doorCollider;
    public Rigidbody doorBody;

    public enum HingeAxis { X, Y, Z }
    public enum HingeSide { Left, Right }
    public bool autoCreatePivot = true;
    public HingeAxis hingeAxis = HingeAxis.Y; // 常见门沿Y轴旋转
    public HingeSide hingeSide = HingeSide.Left; // 左/右边为轴

    public enum SwingDirection { Auto, Inward, Outward }
    public SwingDirection swing = SwingDirection.Outward; // 默认外开

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Coroutine anim;

    void Awake()
    {
        if (pivot == null) pivot = transform;

        if (autoCreatePivot)
        {
            EnsurePivotAtHinge();
        }

        closedRotation = pivot.localRotation;

        if (autoSetupPhysics)
        {
            if (doorCollider == null)
            {
                doorCollider = pivot.GetComponent<Collider>();
                if (doorCollider == null) doorCollider = pivot.GetComponentInChildren<Collider>();
            }

            if (doorCollider == null)
            {
                Debug.LogWarning($"[Door] {name} 没有 Collider，门将无法阻挡玩家。请为门添加 BoxCollider 或 MeshCollider，并确保不是 Trigger。");
            }
            else
            {
                if (doorBody == null)
                {
                    doorBody = pivot.GetComponent<Rigidbody>();
                    if (doorBody == null) doorBody = pivot.gameObject.AddComponent<Rigidbody>();
                }
                doorBody.isKinematic = true;
                doorBody.useGravity = false;

                var mc = doorCollider as MeshCollider;
                if (mc != null && !mc.convex)
                {
                    // 移动的 MeshCollider 需要 convex 才能与刚体一起稳定参与碰撞
                    mc.convex = true;
                }
            }
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        isOpen = true;
        int sign = GetSwingSign(null);
        Quaternion target = closedRotation * GetAxisRotation(openAngle * sign);
        StartAnim(target);
    }

    public void Close()
    {
        isOpen = false;
        StartAnim(closedRotation);
    }

    // 基于指定方向开门（强制内开/外开）
    public void ToggleWithDirection(SwingDirection dir, Transform reference = null)
    {
        if (isOpen)
        {
            Close();
            return;
        }
        int sign = GetSwingSignInternal(dir, reference);
        isOpen = true;
        Quaternion target = closedRotation * GetAxisRotation(openAngle * sign);
        StartAnim(target);
    }

    // 自动根据参考点（通常是玩家）所在侧决定开门方向
    public void ToggleAuto(Transform reference)
    {
        ToggleWithDirection(SwingDirection.Auto, reference);
    }

    private void StartAnim(Quaternion target)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(target));
    }

    private System.Collections.IEnumerator AnimateTo(Quaternion target)
    {
        while (Quaternion.Angle(pivot.localRotation, target) > 0.1f)
        {
            pivot.localRotation = Quaternion.Slerp(pivot.localRotation, target, Time.deltaTime * openSpeed);
            yield return null;
        }
        pivot.localRotation = target;
        anim = null;
    }

    private Quaternion GetAxisRotation(float angle)
    {
        switch (hingeAxis)
        {
            case HingeAxis.X: return Quaternion.Euler(angle, 0f, 0f);
            case HingeAxis.Y: return Quaternion.Euler(0f, angle, 0f);
            case HingeAxis.Z: return Quaternion.Euler(0f, 0f, angle);
            default: return Quaternion.Euler(0f, angle, 0f);
        }
    }

    private int GetSwingSign(Transform reference)
    {
        return GetSwingSignInternal(swing, reference);
    }

    private int GetSwingSignInternal(SwingDirection dir, Transform reference)
    {
        if (dir == SwingDirection.Inward) return -1;
        if (dir == SwingDirection.Outward) return 1;
        // Auto：根据参考点与门的 forward 的相对位置，选择远离参考点的方向
        if (reference == null) return 1; // 无参考点时默认外开
        Vector3 toRef = (reference.position - pivot.position).normalized;
        float dot = Vector3.Dot(pivot.forward, toRef);
        // dot > 0：参考点在门的 forward 侧，为了远离参考点，选择外开（+1）
        // dot < 0：参考点在门的背面侧，选择内开（-1）
        return dot >= 0f ? 1 : -1;
    }

    private void EnsurePivotAtHinge()
    {
        // 若已自定义了pivot且不是本对象，则不改动
        if (pivot != null && pivot != transform) return;

        // 优先用 BoxCollider 估算宽度与中心
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null)
        {
            box = GetComponentInChildren<BoxCollider>();
        }

        if (box == null)
        {
            Debug.LogWarning($"[Door] {name} 未找到 BoxCollider，无法自动定位铰链枢轴。请添加 BoxCollider 或手动指定 pivot。");
            return;
        }

        // 计算世界空间的铰链位置：沿门的局部左右边缘
        Vector3 localCenter = box.center;
        Vector3 size = box.size;

        float halfWidth;
        Vector3 localOffset;
        // 假设沿 Y 旋转时，左右沿局部 X 偏移；沿 X 旋转时，左右沿局部 Z；沿 Z 旋转时，左右沿局部 X（常见门还是Y）
        switch (hingeAxis)
        {
            case HingeAxis.X:
                halfWidth = size.z * 0.5f;
                localOffset = new Vector3(0f, 0f, hingeSide == HingeSide.Left ? -halfWidth : halfWidth);
                break;
            case HingeAxis.Y:
                halfWidth = size.x * 0.5f;
                localOffset = new Vector3(hingeSide == HingeSide.Left ? -halfWidth : halfWidth, 0f, 0f);
                break;
            case HingeAxis.Z:
                halfWidth = size.x * 0.5f;
                localOffset = new Vector3(hingeSide == HingeSide.Left ? -halfWidth : halfWidth, 0f, 0f);
                break;
            default:
                halfWidth = size.x * 0.5f;
                localOffset = new Vector3(hingeSide == HingeSide.Left ? -halfWidth : halfWidth, 0f, 0f);
                break;
        }

        Vector3 localHinge = localCenter + localOffset;
        Vector3 worldHinge = transform.TransformPoint(localHinge);

        // 创建父级 pivot 并将当前门对象作为其子对象，保持世界变换
        var pivotGO = new GameObject("DoorPivot");
        Transform parent = transform.parent;
        pivotGO.transform.SetParent(parent);
        pivotGO.transform.position = worldHinge;
        pivotGO.transform.rotation = transform.rotation;
        pivotGO.transform.localScale = Vector3.one;

        // 重设父子关系，保留世界坐标
        transform.SetParent(pivotGO.transform, true);

        // 更新引用
        pivot = pivotGO.transform;
    }
}
