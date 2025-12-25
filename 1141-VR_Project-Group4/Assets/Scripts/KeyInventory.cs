using UnityEngine;

// 简单的全局钥匙状态存储。
// 可扩展为多钥匙/多关卡。
public static class KeyInventory
{
    // 第一关钥匙是否已拾取
    public static bool HasLv1Key { get; private set; }
    // 第二关两把钥匙是否已拾取
    public static bool HasLv2Key1 { get; private set; }
    public static bool HasLv2Key2 { get; private set; }
    public static bool HasLv2Both => HasLv2Key1 && HasLv2Key2;

    public static void CollectLv1Key()
    {
        HasLv1Key = true;
        Debug.Log("[KeyInventory] 收集到 lv1-key");
    }

    public static void CollectLv2Key1()
    {
        HasLv2Key1 = true;
        Debug.Log("[KeyInventory] 收集到 lv2-key-1");
    }

    public static void CollectLv2Key2()
    {
        HasLv2Key2 = true;
        Debug.Log("[KeyInventory] 收集到 lv2-key-2");
    }
}
