using UnityEngine;

// 说明：
// - 开局按设定难度生成迷宫
// - 将玩家传送到起点
// - 可通过调用 LoadLevel(2) 切换到第二关（更难）

public class MazeGameManager : MonoBehaviour
{
    [Header("References")]
    public MazeGenerator generator; // 指向挂了 MazeGenerator 的物体（推荐 MazeWithoutlight）

    [Tooltip("玩家对象（未指定时按 Tag=Player 自动查找）")]
    public Transform player;

    [Header("Settings")]
    public MazeGenerator.Difficulty startDifficulty = MazeGenerator.Difficulty.Simple; // 第一关简单
    public float spawnYOffset = 0.6f; // 传送时抬高，避免卡地面

    void Start()
    {
        if (!generator)
        {
            generator = FindObjectOfType<MazeGenerator>();
        }
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
            else {
                var byName = GameObject.Find("Player");
                if (byName) player = byName.transform;
            }
        }

        LoadLevel(startDifficulty);
    }

    public void LoadLevel(MazeGenerator.Difficulty difficulty)
    {
        if (!generator)
        {
            Debug.LogError("MazeGameManager: 未找到 MazeGenerator");
            return;
        }

        generator.Generate(difficulty);
        TeleportPlayerToStart();
    }

    public void LoadLevel(int levelIndex)
    {
        var diff = levelIndex <= 1 ? MazeGenerator.Difficulty.Simple : MazeGenerator.Difficulty.Hard;
        LoadLevel(diff);
    }

    void TeleportPlayerToStart()
    {
        if (!player || !generator) return;

        Vector3 target = generator.GetStartWorldPosition(spawnYOffset);

        var cc = player.GetComponent<CharacterController>();
        if (cc)
        {
            // 先禁用避免 Move 限制
            bool prev = cc.enabled;
            cc.enabled = false;
            player.position = target;
            cc.enabled = prev;
        }
        else
        {
            player.position = target;
        }

        // 重置朝向：面向 +Z 方向（可按需改成沿通道方向）
        player.rotation = Quaternion.identity;
    }
}
