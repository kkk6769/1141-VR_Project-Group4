using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

// 把本脚本挂到传送区的Prefab上（该Prefab的Tag需设为 "tp-to-lv1"）。
// 要求：传送区上有 Collider 并勾选 Is Trigger。
// 功能：玩家踩入后，随机传送到三个可配置的目标坐标（或Transform）。
[DisallowMultipleComponent]
public class TTeleportToEndroom : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("本传送区应设置的Tag（用于自检）")] public string requiredTag = "tp-to-lv1";
    [Tooltip("是否要求进入者具有指定玩家Tag")] public bool requirePlayerTag = true;
    [Tooltip("玩家的Tag名称")] public string playerTag = "Player";
    [Tooltip("同一玩家短时间内重复触发的冷却秒数")] public float cooldownSeconds = 1.0f;

    [Header("Destinations (任选其一)")]
    [Tooltip("三个可配置的目标点（Transform），若设置则优先使用其位置/旋转")] public Transform[] targetPoints = new Transform[3];
    [Tooltip("或直接配置三个坐标（Vector3），当对应Transform为空时使用该坐标")] public Vector3[] targetPositions = new Vector3[3];
    [Tooltip("传送后是否对齐至目标的旋转（仅当目标为Transform时有效）")] public bool alignRotationToTarget = false;

    public enum RandomMode { UnityRandom, CryptoRandom, ShuffleBagFair }
    [Header("Randomness")]
    [Tooltip("随机模式：Unity内置、加密随机、或公平洗牌（避免长期偏差）")] public RandomMode randomMode = RandomMode.CryptoRandom;
    [Tooltip("避免同一玩家连续传送到同一个目标")] public bool avoidImmediateRepeat = true;

    [Header("CharacterController Handling")]
    [Tooltip("传送前暂时禁用玩家的CharacterController以避免位置被回弹")]
    public bool temporarilyDisableCharacterController = true;

    // 记录每个进入者的最近传送时间，避免连触发
    private Dictionary<Transform, float> lastTeleportAt = new Dictionary<Transform, float>();
    // 记录每个进入者上次传送的目标索引，用于避免立即重复
    private Dictionary<Transform, int> lastIndexByActor = new Dictionary<Transform, int>();
    // 公平洗牌的袋子：装载所有目标索引并随机打乱，无放回逐个取，空了再重置
    private List<int> shuffleBag = new List<int>(3);

    void Awake()
    {
        // 自检Tag
        if (!string.IsNullOrEmpty(requiredTag) && !CompareTag(requiredTag))
        {
            Debug.LogWarning($"[TeleportToLv1] {name} 的Tag不是 '{requiredTag}'，请在Inspector中设置。");
        }

        // 自检Collider
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"[TeleportToLv1] {name} 缺少触发器Collider或未勾选Is Trigger，传送将无法触发。");
        }

        // 初始化洗牌袋
        ResetShuffleBag();
    }

    void OnTriggerEnter(Collider other)
    {
        // 过滤进入者
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;

        Transform actorRoot = other.transform.root;
        if (!CanTeleport(actorRoot)) return;

        // 随机选择一个目标（使用所选随机模式）
        int index = PickRandomIndex(actorRoot);
        Vector3 destPos = GetDestinationPosition(index);
        Quaternion destRot = GetDestinationRotation(index, actorRoot);

        // 执行传送
        TeleportActor(actorRoot, destPos, destRot);
        lastTeleportAt[actorRoot] = Time.time;
        lastIndexByActor[actorRoot] = index;
    }

    bool CanTeleport(Transform actor)
    {
        if (actor == null) return false;
        if (!lastTeleportAt.TryGetValue(actor, out float last)) return true;
        return (Time.time - last) >= cooldownSeconds;
    }

    int PickRandomIndex(Transform actor)
    {
        int idx = 0;
        switch (randomMode)
        {
            case RandomMode.UnityRandom:
                idx = UnityEngine.Random.Range(0, 3);
                break;
            case RandomMode.CryptoRandom:
                idx = CryptoRandomInt(3);
                break;
            case RandomMode.ShuffleBagFair:
                if (shuffleBag.Count == 0) ResetShuffleBag();
                idx = shuffleBag[0];
                shuffleBag.RemoveAt(0);
                break;
        }

        // 避免对同一玩家的立即重复（尽量重选一次或从洗牌袋中取下一个）
        if (avoidImmediateRepeat && lastIndexByActor.TryGetValue(actor, out int last) && idx == last)
        {
            if (randomMode == RandomMode.ShuffleBagFair)
            {
                if (shuffleBag.Count > 0)
                {
                    // 使用下一个，并把当前候选放回队尾以保持公平
                    int next = shuffleBag[0];
                    shuffleBag.RemoveAt(0);
                    shuffleBag.Add(idx);
                    idx = next;
                }
                else
                {
                    // 袋子空了，重置再取一个
                    ResetShuffleBag();
                    idx = shuffleBag[0];
                    shuffleBag.RemoveAt(0);
                }
            }
            else
            {
                // 尝试重掷一次（最多几次以防无限循环）
                for (int tries = 0; tries < 4; tries++)
                {
                    int newIdx = (randomMode == RandomMode.CryptoRandom) ? CryptoRandomInt(3) : UnityEngine.Random.Range(0, 3);
                    if (newIdx != last)
                    {
                        idx = newIdx;
                        break;
                    }
                }
            }
        }
        return idx;
    }

    void ResetShuffleBag()
    {
        shuffleBag.Clear();
        shuffleBag.Add(0);
        shuffleBag.Add(1);
        shuffleBag.Add(2);
        // Fisher-Yates 洗牌，使用加密随机以尽量接近真实随机
        for (int i = shuffleBag.Count - 1; i > 0; i--)
        {
            int j = CryptoRandomInt(i + 1); // 0..i
            int tmp = shuffleBag[i];
            shuffleBag[i] = shuffleBag[j];
            shuffleBag[j] = tmp;
        }
    }

    int CryptoRandomInt(int maxExclusive)
    {
        if (maxExclusive <= 1) return 0;
        // 无偏采样：拒绝超过最大可整除边界的值
        uint bound = (uint)maxExclusive;
        uint limit = uint.MaxValue - (uint.MaxValue % bound);
        var bytes = new byte[4];
        while (true)
        {
            RandomNumberGenerator.Fill(bytes);
            uint value = System.BitConverter.ToUInt32(bytes, 0);
            if (value < limit)
            {
                return (int)(value % bound);
            }
        }
    }

    Vector3 GetDestinationPosition(int i)
    {
        // 优先Transform
        if (targetPoints != null && i < targetPoints.Length)
        {
            Transform t = targetPoints[i];
            if (t != null) return t.position;
        }
        // 回退到Vector3坐标
        if (targetPositions != null && i < targetPositions.Length)
        {
            return targetPositions[i];
        }
        // 若都未配置，返回原地（并警告）
        Debug.LogWarning($"[TeleportToLv1] 未配置第{i}个目标，传送位置将使用当前点。");
        return transform.position;
    }

    Quaternion GetDestinationRotation(int i, Transform actor)
    {
        if (alignRotationToTarget && targetPoints != null && i < targetPoints.Length)
        {
            Transform t = targetPoints[i];
            if (t != null) return t.rotation;
        }
        return actor.rotation; // 保留原旋转
    }

    void TeleportActor(Transform actorRoot, Vector3 destPos, Quaternion destRot)
    {
        CharacterController cc = actorRoot.GetComponent<CharacterController>();
        bool disabled = false;
        if (temporarilyDisableCharacterController && cc != null && cc.enabled)
        {
            cc.enabled = false;
            disabled = true;
        }

        actorRoot.position = destPos;
        actorRoot.rotation = destRot;

        if (disabled) cc.enabled = true;
    }
}
