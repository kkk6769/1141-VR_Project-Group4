using System.Collections.Generic;
using UnityEngine;

// 说明：
// - 挂在到你的迷宫容器（例如 MazeWithoutlight）或任意空物体上
// - 使用其子物体中的大地板（如 Floor_Clone_）作为地板（不克隆，只复用现有大地板）
// - 使用 wall/Wall_Clone_ 作为“小墙块”原型批量实例化以画出迷宫墙体
// - 生成两种难度迷宫（简单/困难），并预留入口与出口；忽略 SquareLight_Clone_
// - 自动补齐碰撞：大地板缺失碰撞时添加 MeshCollider，墙块实例缺失碰撞时添加 BoxCollider

public class MazeGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("迷宫根节点，建议指向 MazeWithoutlight。若为空默认使用本对象。")]
    public Transform mazeRoot;

    [Tooltip("大地板（例如 MazeWithoutlight/Floor_Clone_）。仅复用，不会被克隆或禁用，可留空。")]
    public Transform bigFloor;

    [Tooltip("墙体原型（例如 MazeWithoutlight/wall/Wall_Clone_）")]
    public Transform wallPrototype;

    [Header("Level Sizes (odd numbers recommended)")]
    public int simpleWidth = 15;
    public int simpleHeight = 15;
    public int hardWidth = 31;
    public int hardHeight = 31;

    [Header("Heights")]
    [Tooltip("地板的世界/本地高度（相对 mazeRoot 的本地 y）。所有墙体基座将放在该高度上方。")]
    public float floorHeight = 13.75f;
    [Tooltip("将墙体直接生成在地平线 y=0（相对 mazeRoot 本地坐标）。启用后忽略 floorHeight。")]
    public bool placeAtGroundY = true;

    [Header("Offsets")]
    [Tooltip("生成网格的原点偏移（相对 mazeRoot 的本地坐标）")]
    public Vector3 localOrigin = Vector3.zero;

    // 运行时数据
    private bool[,] walkable; // true 表示可走，false 表示墙
    private int width, height;
    private Transform generatedParent;

    // 单元大小（根据原型渲染器估测）
    private float tileSizeX = 1f;
    private float tileSizeZ = 1f;
    private float floorY = 0f; // 最终用于放置与传送的 y，高度来源于 floorHeight
    private float wallY = 0f;  // 已不再使用原型的 y，用于兼容旧逻辑

    // 起点/终点（网格坐标）
    private Vector2Int startCell;
    private Vector2Int exitCell;

    public enum Difficulty { Simple = 1, Hard = 2 }

    void Awake()
    {
        if (!mazeRoot) mazeRoot = transform;
        TryAutoBindPrototypes();
        ComputeTileSizeAndHeights();
        EnsureEnvironmentColliders();
    }

    void TryAutoBindPrototypes()
    {
        if (!bigFloor)
        {
            var floor = mazeRoot.Find("Floor_Clone_");
            if (floor) bigFloor = floor as Transform;
        }
        if (!wallPrototype)
        {
            var wallFolder = mazeRoot.Find("wall");
            if (wallFolder)
            {
                var wall = wallFolder.Find("Wall_Clone_");
                if (wall) wallPrototype = wall as Transform;
            }
        }
        // 仅禁用小墙块原型以便克隆；地板是大块，保持启用以复用
        if (wallPrototype) wallPrototype.gameObject.SetActive(false);
    }

    void ComputeTileSizeAndHeights()
    {
        // 高度策略：若启用贴地，则使用 y=0；否则使用固定 floorHeight
        floorY = placeAtGroundY ? 0f : floorHeight;
        if (wallPrototype)
        {
            // 使用小墙块的渲染尺寸作为网格单元大小
            var rd = wallPrototype.GetComponentInChildren<Renderer>();
            if (rd) {
                var size = rd.bounds.size; // 世界尺寸
                tileSizeX = Mathf.Max(0.01f, size.x);
                tileSizeZ = Mathf.Max(0.01f, size.z);
            }
            // 兼容字段保留，但不使用原型的本地 y 作为墙体高度
            wallY = floorY;
        }
    }

    public void Generate(Difficulty difficulty)
    {
        if (!wallPrototype)
        {
            Debug.LogError("MazeGenerator: 请在 Inspector 中指定 wallPrototype（或确保自动绑定成功）");
            return;
        }

        // 配置尺寸（使用奇数更适合迷宫雕刻）
        width  = Mathf.Max(5, (difficulty == Difficulty.Simple ? simpleWidth  : hardWidth));
        height = Mathf.Max(5, (difficulty == Difficulty.Simple ? simpleHeight : hardHeight));
        if (width % 2 == 0) width += 1;
        if (height % 2 == 0) height += 1;

        // 清理旧生成
        ClearGenerated();
        generatedParent = new GameObject("Generated").transform;
        generatedParent.SetParent(mazeRoot, false);
        generatedParent.localPosition = Vector3.zero;

        // 生成网格（DFS 回溯雕刻）
        CarveMazeDFS();

        // 预留入口/出口：边界开口
        startCell = new Vector2Int(1, 1);
        exitCell  = new Vector2Int(width - 2, height - 2);
        walkable[0, 1] = true;                 // 左边界开口 = 门
        walkable[width - 1, height - 2] = true; // 右边界开口 = 出口

        // 实例化墙体（仅为不可走单元放置墙块）
        BuildWallsFromGrid();
    }

    void CarveMazeDFS()
    {
        walkable = new bool[width, height];
        // 先将奇数坐标作为潜在通道
        for (int x = 1; x < width; x += 2)
            for (int y = 1; y < height; y += 2)
                walkable[x, y] = true;

        var stack = new Stack<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var start = new Vector2Int(1, 1);
        stack.Push(start);
        visited.Add(start);

        var dirs = new Vector2Int[] {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };
        var rng = new System.Random();

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            // 寻找未访问的相邻奇数单元（隔一个墙）
            var neighbors = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                var n = current + d * 2;
                if (n.x > 0 && n.x < width - 1 && n.y > 0 && n.y < height - 1 && !visited.Contains(n))
                    neighbors.Add(n);
            }

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            // 随机挑选一个，打通中间墙
            var next = neighbors[rng.Next(neighbors.Count)];
            var between = current + (next - current) / 2;
            walkable[between.x, between.y] = true; // 打通
            visited.Add(next);
            stack.Push(next);
        }
    }

    void BuildWallsFromGrid()
    {
        // 仅为每一个 !walkable 放置墙块；地板复用现有大地板
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 localPos = localOrigin + new Vector3(x * tileSizeX, 0f, y * tileSizeZ);
                if (!walkable[x, y])
                {
                    InstantiateFromProto(wallPrototype, localPos + Vector3.up * floorY);
                }
            }
        }
    }

    void InstantiateFromProto(Transform proto, Vector3 localPos)
    {
        var obj = Instantiate(proto, generatedParent);
        obj.gameObject.SetActive(true);
        obj.localPosition = localPos;
        obj.localRotation = proto.localRotation;
        obj.localScale = proto.localScale;

        // 若实例缺少碰撞体，为墙块添加 BoxCollider（适用于立方体/矩形块）
        if (!obj.GetComponent<Collider>())
        {
            obj.gameObject.AddComponent<BoxCollider>();
        }
    }

    void ClearGenerated()
    {
        var old = mazeRoot.Find("Generated");
        if (old) DestroyImmediate(old.gameObject);
    }

    // 获取起点/终点的世界坐标
    public Vector3 GetStartWorldPosition(float yOffset = 0.5f)
    {
        if (mazeRoot == null) mazeRoot = transform;
        Vector3 local = localOrigin + new Vector3(startCell.x * tileSizeX, floorY + yOffset, startCell.y * tileSizeZ);
        return mazeRoot.TransformPoint(local);
    }

    public Vector3 GetExitWorldPosition(float yOffset = 0.5f)
    {
        if (mazeRoot == null) mazeRoot = transform;
        Vector3 local = localOrigin + new Vector3(exitCell.x * tileSizeX, floorY + yOffset, exitCell.y * tileSizeZ);
        return mazeRoot.TransformPoint(local);
    }

    // 确保环境拥有可碰撞体：为大地板补 MeshCollider（静态，不凸），避免玩家穿透
    void EnsureEnvironmentColliders()
    {
        if (bigFloor)
        {
            var col = bigFloor.GetComponent<Collider>();
            if (!col)
            {
                var mc = bigFloor.gameObject.AddComponent<MeshCollider>();
                mc.convex = false; // 静态几何不需要凸
            }
        }
    }
}
