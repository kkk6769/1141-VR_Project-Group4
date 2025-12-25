using UnityEngine;

// 挂在需要钥匙才能打开的门上（Tag=door1）。
// 同对象或子对象上需有 Door 组件负责动画与旋转。
[DisallowMultipleComponent]
public class LockedDoor : MonoBehaviour
{
    [Tooltip("门的动画组件（如果为空，会在本对象或子对象中查找）")] public Door door;
    [Tooltip("需要的钥匙标识（当前仅lv1-key）")] public string requiredKeyTag = "lv1-key";
    [Tooltip("是否已解锁（会被KeyInventory自动更新）")] public bool unlocked = false;

    void Awake()
    {
        if (door == null)
        {
            door = GetComponent<Door>();
            if (door == null) door = GetComponentInChildren<Door>();
        }
        if (door == null)
        {
            Debug.LogWarning($"[LockedDoor] {name} 未找到 Door 组件，无法执行开门动画。");
        }
    }

    // 供交互脚本调用：尝试开关门
    public void TryToggle()
    {
        if (!IsUnlocked())
        {
            Debug.Log("[LockedDoor] 门已上锁，需要钥匙: " + requiredKeyTag);
            return;
        }
        if (door != null) door.Toggle();
    }

    public bool IsUnlocked()
    {
        // 优先基于门对象的标签判定（推荐做法）
        if (CompareTag("door2"))
        {
            unlocked = KeyInventory.HasLv2Both || unlocked;
            return unlocked;
        }
        if (CompareTag("door1") || CompareTag("door"))
        {
            unlocked = KeyInventory.HasLv1Key || unlocked;
            return unlocked;
        }

        // 兼容：基于 requiredKeyTag 明确指定判定方式
        if (requiredKeyTag == "lv2-both" || requiredKeyTag == "door2")
        {
            unlocked = KeyInventory.HasLv2Both || unlocked;
            return unlocked;
        }
        if (requiredKeyTag == "lv1-key")
        {
            unlocked = KeyInventory.HasLv1Key || unlocked;
            return unlocked;
        }

        // 若未匹配任何规则，保留当前 unlocked 状态
        return unlocked;
    }
}
