using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

// 挂到传送区 Prefab（该 Prefab 的 Tag 设为 "tp-to-lv2"）。
// 传送区需有 Collider 并勾选 Is Trigger。
// 玩家进入后随机传送到两个可配置的目标点（Transform 或 Vector3）。
[DisallowMultipleComponent]
public class TeleportToLv2 : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("传送区自身 Tag（自检用）")] public string requiredTag = "tp-to-lv2";
    [Tooltip("是否要求进入者具有指定玩家Tag")] public bool requirePlayerTag = true;
    [Tooltip("玩家的Tag名称")] public string playerTag = "Player";
    [Tooltip("同一玩家短时间内重复触发的冷却秒数")] public float cooldownSeconds = 1.0f;

    [Header("Destinations (2 points)")]
    [Tooltip("两个目标点（Transform），若设置则优先用其位置/旋转")] public Transform[] targetPoints = new Transform[2];
    [Tooltip("或直接配置两个坐标（Vector3），当对应Transform为空时使用该坐标")] public Vector3[] targetPositions = new Vector3[2];
    [Tooltip("传送后是否对齐至目标的旋转（仅当目标为Transform时有效）")] public bool alignRotationToTarget = false;

    public enum RandomMode { UnityRandom, CryptoRandom, ShuffleBagFair }
    [Header("Randomness")]
    [Tooltip("随机模式：Unity内置、加密随机、或公平洗牌（避免长期偏差）")] public RandomMode randomMode = RandomMode.CryptoRandom;
    [Tooltip("避免同一玩家连续传送到同一个目标")] public bool avoidImmediateRepeat = true;

    [Header("CharacterController Handling")]
    [Tooltip("传送前暂时禁用玩家的CharacterController以避免位置被回弹")] public bool temporarilyDisableCharacterController = true;

    // 记录每个进入者的最近传送时间、上次目标索引
    private Dictionary<Transform, float> lastTeleportAt = new Dictionary<Transform, float>();
    private Dictionary<Transform, int> lastIndexByActor = new Dictionary<Transform, int>();

    // 公平洗牌袋：索引 0/1 无放回抽取，用尽再洗牌
    private List<int> shuffleBag = new List<int>(2);

    const int DestCount = 2;

    void Awake()
    {
        // 自检Tag
        if (!string.IsNullOrEmpty(requiredTag) && !CompareTag(requiredTag))
        {
            Debug.LogWarning($"[TeleportToLv2] {name} 的Tag不是 '{requiredTag}'，请在Inspector中设置。");
        }
        // 自检Collider
        var col = GetComponent<Collider>();
        if (col == null || !col.isTrigger)
        {
            Debug.LogWarning($"[TeleportToLv2] {name} 缺少触发器Collider或未勾选Is Trigger，传送将无法触发。");
        }
        ResetShuffleBag();
    }

    void OnTriggerEnter(Collider other)
    {
        if (requirePlayerTag && !other.CompareTag(playerTag)) return;
        Transform actorRoot = other.transform.root;
        if (!CanTeleport(actorRoot)) return;

        int index = PickRandomIndex(actorRoot);
        Vector3 destPos = GetDestinationPosition(index);
        Quaternion destRot = GetDestinationRotation(index, actorRoot);

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
                idx = UnityEngine.Random.Range(0, DestCount);
                break;
            case RandomMode.CryptoRandom:
                idx = CryptoRandomInt(DestCount);
                break;
            case RandomMode.ShuffleBagFair:
                if (shuffleBag.Count == 0) ResetShuffleBag();
                idx = shuffleBag[0];
                shuffleBag.RemoveAt(0);
                break;
        }
        if (avoidImmediateRepeat && lastIndexByActor.TryGetValue(actor, out int last) && idx == last)
        {
            if (randomMode == RandomMode.ShuffleBagFair)
            {
                if (shuffleBag.Count > 0)
                {
                    int next = shuffleBag[0];
                    shuffleBag.RemoveAt(0);
                    shuffleBag.Add(idx);
                    idx = next;
                }
                else
                {
                    ResetShuffleBag();
                    idx = shuffleBag[0];
                    shuffleBag.RemoveAt(0);
                }
            }
            else
            {
                for (int tries = 0; tries < 4; tries++)
                {
                    int newIdx = (randomMode == RandomMode.CryptoRandom) ? CryptoRandomInt(DestCount) : UnityEngine.Random.Range(0, DestCount);
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
        for (int i = 0; i < DestCount; i++) shuffleBag.Add(i);
        // Fisher-Yates 洗牌（使用加密随机）
        for (int i = shuffleBag.Count - 1; i > 0; i--)
        {
            int j = CryptoRandomInt(i + 1);
            int tmp = shuffleBag[i];
            shuffleBag[i] = shuffleBag[j];
            shuffleBag[j] = tmp;
        }
    }

    int CryptoRandomInt(int maxExclusive)
    {
        if (maxExclusive <= 1) return 0;
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
        if (targetPoints != null && i < targetPoints.Length)
        {
            Transform t = targetPoints[i];
            if (t != null) return t.position;
        }
        if (targetPositions != null && i < targetPositions.Length)
        {
            return targetPositions[i];
        }
        Debug.LogWarning($"[TeleportToLv2] 未配置第{i}个目标，使用当前点。");
        return transform.position;
    }

    Quaternion GetDestinationRotation(int i, Transform actor)
    {
        if (alignRotationToTarget && targetPoints != null && i < targetPoints.Length)
        {
            Transform t = targetPoints[i];
            if (t != null) return t.rotation;
        }
        return actor.rotation;
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
