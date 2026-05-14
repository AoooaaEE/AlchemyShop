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

        private static Sprite _sun;
        private static Sprite _star;
        private static Sprite _pineTree;
        private static Sprite _spark;
        private static Sprite _cardBurst;

        /// <summary>Солнце с короной: ярко-белая середина → жёлтое ядро → мягкий ореол. Используется как фоновый диск.</summary>
        public static Sprite Sun(int size = 128)
        {
            if (_sun != null) return _sun;
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
                Color c;
                if (d > 1f) c = new Color(0f, 0f, 0f, 0f);
                else if (d < 0.55f) c = new Color(1f, 1f, 0.85f, 1f);          // ядро
                else if (d < 0.75f) c = new Color(1f, 0.92f, 0.55f, 1f);       // жёлтый
                else                c = new Color(1f, 0.85f, 0.45f, Mathf.Clamp01(1f - (d - 0.75f) / 0.25f) * 0.6f); // ореол
                px[y * size + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            _sun = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _sun.name = "SE_Sun";
            return _sun;
        }

        /// <summary>Маленькая 4-конечная звёздочка (тонкий крест с заострением).</summary>
        public static Sprite Star(int size = 32)
        {
            if (_star != null) return _star;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - r + 0.5f) / r;
                float dy = Mathf.Abs(y - r + 0.5f) / r;
                // «Крест»: внутри одной из узких полос
                float coreH = Mathf.Clamp01(1f - dx * 3f) * Mathf.Clamp01(1f - dy * 1.2f);
                float coreV = Mathf.Clamp01(1f - dy * 3f) * Mathf.Clamp01(1f - dx * 1.2f);
                float a = Mathf.Max(coreH, coreV);
                a = a * a;
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            _star = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _star.name = "SE_Star";
            return _star;
        }

        /// <summary>Силуэт ели: треугольник тёмной зелени с маленьким стволиком. Pivot — низ-центр.</summary>
        public static Sprite PineTree(int size = 64)
        {
            if (_pineTree != null) return _pineTree;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            int cx = size / 2;
            // Делаем ствол (нижние 15%)
            int trunkTop = Mathf.RoundToInt(size * 0.15f);
            int trunkHalf = Mathf.Max(1, size / 16);
            // Делаем ёлку из 3 ярусов треугольников
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Color c = new Color(0f, 0f, 0f, 0f);
                if (y < trunkTop && Mathf.Abs(x - cx) <= trunkHalf)
                {
                    c = new Color(0.4f, 0.27f, 0.15f, 1f); // ствол
                }
                else
                {
                    // 3 яруса, каждый — треугольник снизу-вверх, перекрывающийся
                    for (int tier = 0; tier < 3; tier++)
                    {
                        float tierBottom = trunkTop + tier * (size - trunkTop) / 4f;
                        float tierTop = trunkTop + (tier + 2) * (size - trunkTop) / 4f;
                        if (y >= tierBottom && y <= tierTop)
                        {
                            float t = (y - tierBottom) / (tierTop - tierBottom);
                            int halfW = Mathf.RoundToInt((1f - t) * (size * (0.45f - tier * 0.06f)));
                            if (Mathf.Abs(x - cx) <= halfW)
                            {
                                c = new Color(1f, 1f, 1f, 1f);
                                break;
                            }
                        }
                    }
                }
                px[y * size + x] = c;
            }
            tex.SetPixels(px);
            tex.Apply();
            _pineTree = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0f), size);
            _pineTree.name = "SE_PineTree";
            return _pineTree;
        }

        /// <summary>Блестящая ромб-звезда для редких карт, магии и HUD.</summary>
        public static Sprite Spark(int size = 48)
        {
            if (_spark != null) return _spark;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - r + 0.5f) / r;
                float dy = Mathf.Abs(y - r + 0.5f) / r;
                float diamond = Mathf.Clamp01(1f - (dx + dy));
                float cross = Mathf.Max(Mathf.Clamp01(1f - dx * 6f) * Mathf.Clamp01(1f - dy * 1.5f),
                                        Mathf.Clamp01(1f - dy * 6f) * Mathf.Clamp01(1f - dx * 1.5f));
                float a = Mathf.Max(diamond * diamond, cross * cross * 0.85f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            _spark = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _spark.name = "SE_Spark";
            return _spark;
        }

        /// <summary>Лучистая эмблема для центральных иконок карт.</summary>
        public static Sprite CardBurst(int size = 96)
        {
            if (_cardBurst != null) return _cardBurst;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - r + 0.5f) / r;
                float ny = (y - r + 0.5f) / r;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float angle = Mathf.Atan2(ny, nx);
                float rays = 0.55f + 0.45f * Mathf.Cos(angle * 12f);
                float a = Mathf.Clamp01(1f - d) * rays;
                a = Mathf.Pow(a, 1.35f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            _cardBurst = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _cardBurst.name = "SE_CardBurst";
            return _cardBurst;
        }
    }
}
