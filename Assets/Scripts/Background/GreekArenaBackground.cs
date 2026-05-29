using UnityEngine;

[ExecuteAlways]
public class GreekArenaBackground : MonoBehaviour
{
    // Palette (Hellenic Twilight)
    static readonly Color SkyTop      = HexToColor("#0F1B3D");
    static readonly Color SkyBottom   = HexToColor("#2A3563");
    static readonly Color MountainFar = HexToColor("#1A2447");
    static readonly Color MountainNear= HexToColor("#0F1B3D");
    static readonly Color Marble      = HexToColor("#F4ECD8");
    static readonly Color Gold        = HexToColor("#D4AF37");
    static readonly Color GoldLight   = HexToColor("#FFE082");

    // World size (designed for 16:9 camera with orthographic size 5 -> world 17.78 x 10)
    const float W = 18f;
    const float H = 10f;

    void Start() { Build(); }

    void Build()
    {
        // Clean previous children
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // 1) SKY (gradient quad)
        var sky = MakeQuad("Sky", new Vector2(0f, 1.5f), new Vector2(W, H - 3f), -100);
        ApplyVerticalGradient(sky, SkyTop, SkyBottom, 256);

        // 2) SUN (top-right)
        var sun = MakeCircle("Sun", new Vector2(W * 0.35f, 3.2f), 0.9f, GoldLight, -90);
        MakeCircle("SunGlow", new Vector2(W * 0.35f, 3.2f), 1.5f, new Color(GoldLight.r, GoldLight.g, GoldLight.b, 0.25f), -91);

        // 3) MOUNTAINS FAR (5 triangles)
        MakeTriangleRange("MountainsFar", MountainFar,
            baseY: -1.5f, peakYMin: 1.8f, peakYMax: 3.0f,
            spread: W, count: 5, orderLayer: -80, seed: 7);

        // 4) MOUNTAINS NEAR (3 bigger darker)
        MakeTriangleRange("MountainsNear", MountainNear,
            baseY: -2.0f, peakYMin: 1.0f, peakYMax: 2.2f,
            spread: W, count: 4, orderLayer: -70, seed: 13);

        // 5) FLOOR (marble slab)
        MakeQuad("Floor", new Vector2(0f, -3.5f), new Vector2(W, 3f), -60, Marble);

        // 6) MEANDER GOLD LINE (top edge of floor)
        MakeQuad("MeanderLine", new Vector2(0f, -2.0f), new Vector2(W, 0.12f), -55, Gold);
        MakeQuad("MeanderLineThin", new Vector2(0f, -2.18f), new Vector2(W, 0.04f), -55, Gold);

        // 7) COLUMNS (ivory marble, side decoration)
        MakeQuad("ColumnLeft",  new Vector2(-W * 0.5f + 0.5f, 0.5f), new Vector2(0.7f, 6.5f), -65, Marble);
        MakeQuad("ColumnRight", new Vector2( W * 0.5f - 0.5f, 0.5f), new Vector2(0.7f, 6.5f), -65, Marble);
        MakeQuad("ColumnLeftCap",  new Vector2(-W * 0.5f + 0.5f, 3.6f), new Vector2(1.0f, 0.4f), -64, Gold);
        MakeQuad("ColumnRightCap", new Vector2( W * 0.5f - 0.5f, 3.6f), new Vector2(1.0f, 0.4f), -64, Gold);
        MakeQuad("ColumnLeftBase",  new Vector2(-W * 0.5f + 0.5f, -2.6f), new Vector2(1.0f, 0.4f), -64, Gold);
        MakeQuad("ColumnRightBase", new Vector2( W * 0.5f - 0.5f, -2.6f), new Vector2(1.0f, 0.4f), -64, Gold);
    }

    GameObject MakeQuad(string name, Vector2 center, Vector2 size, int order, Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = WhiteSprite();
        sr.color = color ?? Color.white;
        sr.sortingOrder = order;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        return go;
    }

    void ApplyVerticalGradient(GameObject quad, Color top, Color bottom, int resolution)
    {
        var tex = new Texture2D(1, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < resolution; y++)
        {
            float t = (float)y / (resolution - 1);
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        var sr = quad.GetComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, resolution), new Vector2(0.5f, 0.5f), 100);
        sr.color = Color.white;
    }

    GameObject MakeCircle(string name, Vector2 center, float radius, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CircleSprite(128);
        sr.color = color;
        sr.sortingOrder = order;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
        return go;
    }

    void MakeTriangleRange(string parentName, Color color, float baseY, float peakYMin, float peakYMax, float spread, int count, int orderLayer, int seed)
    {
        var parent = new GameObject(parentName);
        parent.transform.SetParent(transform, false);
        var rng = new System.Random(seed);
        float step = spread / count;
        for (int i = 0; i < count; i++)
        {
            float cx = -spread * 0.5f + step * i + step * 0.5f + (float)(rng.NextDouble() - 0.5) * step * 0.3f;
            float width = step * (1.2f + (float)rng.NextDouble() * 0.6f);
            float peakY = peakYMin + (float)rng.NextDouble() * (peakYMax - peakYMin);
            CreateTriangle(parent.transform, $"Peak{i}", new Vector2(cx, baseY), width, peakY - baseY, color, orderLayer + i);
        }
    }

    void CreateTriangle(Transform parent, string name, Vector2 baseCenter, float width, float height, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = baseCenter;
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-width * 0.5f, 0, 0),
            new Vector3( width * 0.5f, 0, 0),
            new Vector3( 0, height, 0)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        mesh.colors = new Color[] { color, color, color };
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        mr.sharedMaterial = mat;
        mr.sortingOrder = order;
    }

    static Sprite _whiteSprite;
    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
        return _whiteSprite;
    }

    static Sprite _circleSprite;
    static Sprite CircleSprite(int res)
    {
        if (_circleSprite != null) return _circleSprite;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float r = res * 0.5f;
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float dx = x - r, dy = y - r;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - (d - r + 1f));
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100);
        return _circleSprite;
    }

    static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");
        byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
        byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
        byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }
}
