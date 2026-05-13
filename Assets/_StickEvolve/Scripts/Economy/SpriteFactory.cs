using UnityEngine;

namespace StickEvolve.Economy
{
    /// <summary>
    /// Делает белые процедурные спрайты для тонирования через SpriteRenderer.color.
    /// Так все объекты прототипа рисуются цветными примитивами без ассетов.
    /// </summary>
    public static class SpriteFactory
    {
        private static Sprite _white;
        private static Sprite _circle;
        private static Sprite _softCircle;
        private static Sprite _triangle;
        private static Sprite _verticalGradient;

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

        /// <summary>Круг с мягким альфа-затуханием по краям. Для облаков и теней.</summary>
        public static Sprite SoftCircle(int size = 128)
        {
            if (_softCircle != null) return _softCircle;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - r + 0.5f) / r;
                float dy = (y - r + 0.5f) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            _softCircle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _softCircle.name = "SE_SoftCircle";
            return _softCircle;
        }

        /// <summary>Равнобедренный треугольник вершиной вверх. Для гор/щитов.</summary>
        public static Sprite Triangle(int size = 64)
        {
            if (_triangle != null) return _triangle;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float t = (float)y / (size - 1);
                int halfWidth = Mathf.RoundToInt((1f - t) * size * 0.5f);
                int cx = size / 2;
                for (int x = 0; x < size; x++)
                {
                    bool inside = x >= cx - halfWidth && x <= cx + halfWidth;
                    px[y * size + x] = inside ? Color.white : new Color(0f, 0f, 0f, 0f);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _triangle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0f), size);
            _triangle.name = "SE_Triangle";
            return _triangle;
        }

        /// <summary>Вертикальный градиент сверху→вниз. Альфа = 1 сверху, 0 снизу. Для неба.</summary>
        public static Sprite VerticalGradient(int height = 256)
        {
            if (_verticalGradient != null) return _verticalGradient;
            var tex = new Texture2D(2, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[2 * height];
            for (int y = 0; y < height; y++)
            {
                float t = 1f - (float)y / (height - 1);
                var c = new Color(1f, 1f, 1f, t);
                px[y * 2] = c;
                px[y * 2 + 1] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            _verticalGradient = Sprite.Create(tex, new Rect(0f, 0f, 2f, height), new Vector2(0.5f, 0.5f), height);
            _verticalGradient.name = "SE_VertGradient";
            return _verticalGradient;
        }
    }
}
