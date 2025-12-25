using UnityEngine;

// 简易屏幕中心准星（黑点或十字），无需额外UI资源。
// 可将其挂到任意对象（推荐挂到玩家），在 Inspector 调整样式。
public class CrosshairUI : MonoBehaviour
{
    public bool enabledCrosshair = true;
    public Color color = Color.black;
    [Tooltip("准星半径（像素）")] public float radius = 3f;
    [Tooltip("十字长度（像素），为0则显示圆点")] public float crossLength = 0f;
    [Tooltip("线宽（像素）")] public float lineWidth = 2f;

    Texture2D circleTex;
    int cachedRadius;
    Color cachedColor;

    void OnGUI()
    {
        if (!enabledCrosshair) return;
        Vector2 center = new Vector2(Screen.width/2f, Screen.height/2f);
        if (crossLength <= 0f)
        {
            // 画圆点（真实圆形纹理，显式启用Alpha混合）
            EnsureCircleTexture();
            Rect r = new Rect(center.x - cachedRadius, center.y - cachedRadius, cachedRadius * 2f, cachedRadius * 2f);
            var prevColor = GUI.color;
            GUI.color = Color.white; // 纹理内已包含颜色，避免再叠加色彩导致不透明
            GUI.DrawTexture(r, circleTex, ScaleMode.StretchToFill, true);
            GUI.color = prevColor;
        }
        else
        {
            // 画十字
            var prevColor = GUI.color;
            GUI.color = color;
            // 横线
            GUI.DrawTexture(new Rect(center.x - crossLength, center.y - lineWidth/2f, crossLength * 2f, lineWidth), Texture2D.whiteTexture);
            // 竖线
            GUI.DrawTexture(new Rect(center.x - lineWidth/2f, center.y - crossLength, lineWidth, crossLength * 2f), Texture2D.whiteTexture);
            GUI.color = prevColor;
        }
    }

    void EnsureCircleTexture()
    {
        int r = Mathf.Max(1, Mathf.RoundToInt(radius));
        if (circleTex != null && r == cachedRadius && color == cachedColor) return;

        cachedRadius = r;
        cachedColor = color;
        int size = r * 2 + 1;
        if (circleTex == null || circleTex.width != size || circleTex.height != size)
        {
            if (circleTex != null) Destroy(circleTex);
            circleTex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            circleTex.wrapMode = TextureWrapMode.Clamp;
            circleTex.filterMode = FilterMode.Point;
            circleTex.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }

        Color32[] pixels = new Color32[size * size];
        float r2 = r * r;
        int cx = r;
        int cy = r;
        Color32 c = (Color32)color;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int idx = y * size + x;
                if (dx * dx + dy * dy <= r2)
                {
                    pixels[idx] = c;
                }
                else
                {
                    pixels[idx] = new Color32(0,0,0,0);
                }
            }
        }
        circleTex.SetPixels32(pixels);
        circleTex.Apply();
    }

    void OnDestroy()
    {
        if (circleTex != null) Destroy(circleTex);
    }
}
