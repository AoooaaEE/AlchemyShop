using UnityEngine;

namespace StickEvolve.Economy
{
    /// <summary>
    /// Делает белый 1x1 спрайт для тонирования через SpriteRenderer.color.
    /// Так все объекты прототипа рисуются цветными прямоугольниками без ассетов.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _white;
        private static Sprite _circle;

        public static Sprite White()
        {
            if (_white != null) return _white;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var px = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(px);
            tex.Apply();
            _white = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
            _white.name = "SE_White";
            return _white;
        }

        public static Sprite Circle(int size = 64)
        {
            if (_circle != null) return _circle;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                bool inside = dx * dx + dy * dy <= (r - 1f) * (r - 1f);
                px[y * size + x] = inside ? Color.white : new Color(0f, 0f, 0f, 0f);
            }
            tex.SetPixels(px);
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _circle.name = "SE_Circle";
            return _circle;
        }
    }
}
