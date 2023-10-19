using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosshairRenderer : Graphic
{
    [Header("Rendering Properties")]
    public Color[] Colors;
    [Range(1600, 12800)]
    public int NumberOfRects = 6400;
    public float CircleRadius = 50f;
    public float Thickness = 5f;

    [Header("UI Manager")]
    public UIManagement UIManager;
    [Header("Settings")]
    public TMP_Dropdown ResDropdown;
    public Slider RadiusSlider;
    public Slider ThicknessSlider;
    [Header("Compute Shader")]
    public ComputeShader shader;

    // Resolution Options
    readonly int[] _resOptions = { 1600, 6400, 12800 };

    struct Rect
    {
        public Vector2 pos;
        public Vector2 delta1;
        public Vector2 delta2;
    }

    protected override void Awake()
    {
        if (GameManager.CurrentGame.Equals("Portal")) OnPopulateMesh(new VertexHelper());
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (UIManager && UIManager.InCross())
        {
            DrawCircle(vh, 200f);
        }
        else if (!UIManagement.IsPaused())
        {
            DrawCircle(vh, 0f);
        }
    }

    void DrawCircle(VertexHelper vh, float yOffset)
    {
        // Circle/Rect Properties
        float angleOffset = 2 * Mathf.PI / NumberOfRects;
        float rectWidth = Thickness;
        float rectLength = Mathf.Sqrt(2 * Mathf.Pow(CircleRadius, 2) * (1 - Mathf.Cos(angleOffset)));

        Vector2 uv0 = Vector2.zero;

        // BIG BIG THIS IS HUGE !!!!!!!!!!!
        int size = sizeof(float) * 6;
        ComputeBuffer buffer = new(NumberOfRects, size);
        Rect[] data = new Rect[NumberOfRects];
        buffer.SetData(data);

        shader.SetBuffer(0, "Rects", buffer);
        shader.SetFloat(Shader.PropertyToID("angleOffset"), angleOffset);
        shader.SetFloat(Shader.PropertyToID("rectWidth"), rectWidth);
        shader.SetFloat(Shader.PropertyToID("rectLength"), rectLength);
        shader.SetFloat(Shader.PropertyToID("yOffset"), yOffset);
        shader.SetFloat(Shader.PropertyToID("circRadius"), CircleRadius);

        shader.Dispatch(0, NumberOfRects / 64, 1, 1);
        buffer.GetData(data);

        // Set Vertices/Triangles
        int idx = 0;
        int colorIncrementIdx = NumberOfRects / Colors.Length;
        for (int i = 0; i < NumberOfRects; i++)
        {
            Rect rect = data[i];
            int color_idx = i / colorIncrementIdx;
            Color col = Colors[color_idx];
            vh.AddVert(rect.pos, col, uv0);
            vh.AddVert(rect.pos + rect.delta2, col, uv0);
            vh.AddVert(rect.pos + rect.delta1 + rect.delta2, col, uv0);
            vh.AddVert(rect.pos + rect.delta1, col, uv0);

            idx = i * 4;
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx, idx + 3, idx + 2);
            if (i > 0)
            {
                vh.AddTriangle(idx, idx - 2, idx + 1);
            }
        }
        // Add Final triangle to fill in gap
        vh.AddTriangle(0, 1, idx - 2);

        buffer.Dispose();
    }

    public void DarkenColor(int idx) { Colors[idx] -= new Color(0.5f, 0.5f, 0.5f, 0); }
    public void BrightenColor(int idx) { Colors[idx] += new Color(0.5f, 0.5f, 0.5f, 0); }

    public void ChangeResolution()
    {
        NumberOfRects = _resOptions[ResDropdown.value];
        SetAllDirty();
    }

    public void ChangeRadius()
    {
        CircleRadius = RadiusSlider.value;
        SetAllDirty();
    }

    public void ChangeThickness()
    {
        Thickness = ThicknessSlider.value;
        SetAllDirty();
    }
}
