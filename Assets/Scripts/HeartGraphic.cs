using UnityEngine;
using UnityEngine.UI;

// Coeur en pixels, dessine directement dans le HUD sans asset supplementaire.
public class HeartGraphic : MaskableGraphic
{
    private static readonly string[] Pixels = {
        "01100110", "11111111", "11111111", "11111111",
        "01111110", "00111100", "00011000"
    };
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float unit = Mathf.Min(rect.width / 8f, rect.height / 7f);
        for (int y = 0; y < 7; y++)
        for (int x = 0; x < 8; x++)
        {
            if (Pixels[y][x] != '1') continue;
            float left = rect.xMin + x * unit;
            float top = rect.yMax - y * unit;
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(left, top - unit), color, Vector2.zero);
            vh.AddVert(new Vector3(left, top), color, Vector2.zero);
            vh.AddVert(new Vector3(left + unit, top), color, Vector2.zero);
            vh.AddVert(new Vector3(left + unit, top - unit), color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
