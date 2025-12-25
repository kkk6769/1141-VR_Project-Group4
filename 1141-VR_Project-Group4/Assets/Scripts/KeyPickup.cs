using UnityEngine;

// 把本脚本挂到钥匙 Prefab (Tag=lv1-key)。
// 要求：钥匙上有Collider并勾选IsTrigger，玩家Tag默认为"Player"。
[DisallowMultipleComponent]
public class KeyPickup : MonoBehaviour
{
    [Tooltip("要求进入者的Tag")] public string playerTag = "Player";
    [Tooltip("拾取后是否销毁钥匙对象（否则SetActive(false)）")] public bool destroyOnPickup = true;
    [Tooltip("拾取钥匙后需要隐藏/销毁的锁模型Tag（例如 lv1-look）")] public string lockTag = "lv1-look";
    [Tooltip("对锁模型执行销毁（true）或仅隐藏（false）")] public bool destroyLocks = false;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[KeyPickup] {name} 没有 Collider。射线拾取仍可工作，但触发器拾取不可用。");
        }
        else if (!col.isTrigger)
        {
            Debug.Log("[KeyPickup] 提示：如需触发器拾取请勾选 Is Trigger；若仅使用射线+按键拾取，可忽略此提示。");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        Pickup();
    }

    // 显式拾取：供玩家脚本通过射线调用
    public void Pickup()
    {
        string t = gameObject.tag;
        // 根据钥匙Tag记录对应状态
        if (t == "lv2-key-1")
        {
            KeyInventory.CollectLv2Key1();
            HideLocksByTag("lv2-look-1");
        }
        else if (t == "lv2-key-2")
        {
            KeyInventory.CollectLv2Key2();
            HideLocksByTag("lv2-look-2");
        }
        else
        {
            KeyInventory.CollectLv1Key();
            HideLocks();
        }
        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    void HideLocks()
    {
        if (string.IsNullOrEmpty(lockTag)) return;
        var locks = GameObject.FindGameObjectsWithTag(lockTag);
        for (int i = 0; i < locks.Length; i++)
        {
            if (locks[i] == null) continue;
            if (destroyLocks) Destroy(locks[i]);
            else locks[i].SetActive(false);
        }
    }

    void HideLocksByTag(string tagToHide)
    {
        if (string.IsNullOrEmpty(tagToHide)) return;
        var locks = GameObject.FindGameObjectsWithTag(tagToHide);
        for (int i = 0; i < locks.Length; i++)
        {
            if (locks[i] == null) continue;
            if (destroyLocks) Destroy(locks[i]);
            else locks[i].SetActive(false);
        }
    }
}
