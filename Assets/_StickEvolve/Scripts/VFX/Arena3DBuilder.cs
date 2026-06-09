using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// 3D-арена «v2 polish»: плитка на полу + центральный магический круг,
    /// стены с подсветкой снизу, колонны с факелами (точечный мерцающий свет),
    /// параллакс-небо позади, парящие угольки, насыщенный свет.
    /// </summary>
    public static class Arena3DBuilder
    {
        public static void Build(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            BuildSky(arenaRoot, halfWidth, halfHeight);
            BuildFloor(arenaRoot, halfWidth, halfHeight);
            BuildCompositionShapes(arenaRoot, halfWidth, halfHeight);
            BuildMagicCircle(arenaRoot);
            BuildWalls(arenaRoot, halfWidth, halfHeight);
            BuildDecorativeTrim(arenaRoot, halfWidth, halfHeight);
            BuildColumnsAndTorches(arenaRoot, halfWidth, halfHeight);
            BuildSideShrines(arenaRoot, halfWidth, halfHeight);
            BuildAmbientGlowSpots(arenaRoot, halfWidth, halfHeight);

            // Парящие угольки над ареной (две группы — тёплые и холодные).
            EmberParticles.Spawn(arenaRoot, new Vector2(halfWidth * 1.55f, halfHeight * 1.45f),
                new Color(1f, 0.55f, 0.18f), count: 10);
            EmberParticles.Spawn(arenaRoot, new Vector2(halfWidth * 1.55f, halfHeight * 1.45f),
                new Color(0.45f, 0.55f, 1.0f), count: 5);
        }

        private static void BuildFloor(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            // База — большая плита, чтобы не было дыр.
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor3D_Base";
            var fc = floor.GetComponent<Collider>(); if (fc != null) Object.Destroy(fc);
            floor.transform.SetParent(arenaRoot, worldPositionStays: false);
            floor.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            floor.transform.localScale = new Vector3(halfWidth * 2.35f, halfHeight * 2.25f, 1f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial.Get(new Color(0.16f, 0.12f, 0.13f));

            // Плитка: сетка квадов с лёгкой вариацией цвета и швами.
            const float tile = 0.9f;
            int cols = Mathf.CeilToInt(halfWidth * 2f / tile);
            int rows = Mathf.CeilToInt(halfHeight * 2f / tile);
            float startX = -halfWidth + tile * 0.5f;
            float startY = -halfHeight + tile * 0.5f;
            var tileColors = new Color[]
            {
                new Color(0.24f, 0.18f, 0.17f),
                new Color(0.20f, 0.15f, 0.16f),
                new Color(0.28f, 0.20f, 0.18f),
                new Color(0.17f, 0.13f, 0.15f),
            };
            var tilesRoot = new GameObject("FloorTiles");
            tilesRoot.transform.SetParent(arenaRoot, worldPositionStays: false);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    q.name = $"Tile_{x}_{y}";
                    var col = q.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                    q.transform.SetParent(tilesRoot.transform, worldPositionStays: false);
                    q.transform.localPosition = new Vector3(startX + x * tile, startY + y * tile, 0f);
                    q.transform.localScale = new Vector3(tile * 0.92f, tile * 0.92f, 1f);
                    int idx = (x * 7 + y * 13 + (x + y)) % tileColors.Length;
                    var mr = q.GetComponent<MeshRenderer>();
                    mr.sharedMaterial = LitMaterial.Get(tileColors[idx]);
                    mr.receiveShadows = true;
                }
            }

            // «Dead Cells/Hades» деталь: прожилки/трещины с лавовым и холодным свечением.
            BuildFloorCracks(arenaRoot);
        }

        private static void BuildCompositionShapes(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            // Большая читаемая форма в центре: вместо пустой чёрной коробки кадр получает
            // «боевую дорожку» с тёплой Hades-like палитрой. Это не влияет на коллайдеры.
            var runner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            runner.name = "CentralCombatRunner";
            var runnerCol = runner.GetComponent<Collider>(); if (runnerCol != null) Object.Destroy(runnerCol);
            runner.transform.SetParent(arenaRoot, worldPositionStays: false);
            runner.transform.localPosition = new Vector3(0f, 0f, 0.016f);
            runner.transform.localScale = new Vector3(halfWidth * 1.45f, halfHeight * 1.55f, 1f);
            runner.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial.Get(new Color(0.26f, 0.13f, 0.13f));

            Color bronze = new Color(0.72f, 0.42f, 0.18f);
            Character3DBuilder.MakeCube(arenaRoot, "RunnerTrimN", bronze,
                new Vector3(0f, halfHeight * 0.74f, 0.04f), new Vector3(halfWidth * 1.35f, 0.05f, 0.035f));
            Character3DBuilder.MakeCube(arenaRoot, "RunnerTrimS", bronze,
                new Vector3(0f, -halfHeight * 0.74f, 0.04f), new Vector3(halfWidth * 1.35f, 0.05f, 0.035f));
            Character3DBuilder.MakeCube(arenaRoot, "RunnerTrimE", bronze,
                new Vector3(halfWidth * 0.70f, 0f, 0.04f), new Vector3(0.05f, halfHeight * 1.45f, 0.035f));
            Character3DBuilder.MakeCube(arenaRoot, "RunnerTrimW", bronze,
                new Vector3(-halfWidth * 0.70f, 0f, 0.04f), new Vector3(0.05f, halfHeight * 1.45f, 0.035f));
        }

        private static void BuildFloorCracks(Transform arenaRoot)
        {
            void Crack(string name, Vector2 a, Vector2 b, Color c, float width, float emit)
            {
                Vector2 d = b - a;
                float len = d.magnitude;
                if (len <= 0.01f) return;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                var seg = Character3DBuilder.MakeCubeEmissive(arenaRoot, name, c, c, emit,
                    new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0.018f),
                    new Vector3(len, width, 0.025f));
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            Color lava = new Color(1.0f, 0.28f, 0.08f);
            Color arcane = new Color(0.35f, 0.80f, 1.0f);
            Crack("LavaCrack_A", new Vector2(-3.4f, -3.5f), new Vector2(-2.1f, -2.3f), lava, 0.035f, 0.9f);
            Crack("LavaCrack_B", new Vector2(-2.1f, -2.3f), new Vector2(-1.0f, -2.8f), lava, 0.028f, 0.8f);
            Crack("LavaCrack_C", new Vector2(2.9f, 3.4f), new Vector2(1.8f, 2.3f), lava, 0.03f, 0.8f);
            Crack("ArcaneCrack_A", new Vector2(-3.1f, 3.0f), new Vector2(-1.6f, 2.0f), arcane, 0.026f, 0.75f);
        }

        private static void BuildMagicCircle(Transform arenaRoot)
        {
            // Магический круг в центре — несколько концентрических квадов с разным цветом/прозрачностью.
            void Ring(float radius, float thickness, Color c, float emit)
            {
                int segments = 20;
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seg.name = "RuneSeg";
                    var col = seg.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                    seg.transform.SetParent(arenaRoot, worldPositionStays: false);
                    seg.transform.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0.012f);
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Rad2Deg * a);
                    float segLen = 2f * Mathf.PI * radius / segments * 0.85f;
                    seg.transform.localScale = new Vector3(segLen, thickness, 0.02f);
                    seg.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial.GetEmissive(c, emit);
                }
            }
            Ring(1.35f, 0.045f, new Color(0.45f, 0.65f, 1.0f), 0.65f);
            Ring(0.95f, 0.035f, new Color(0.95f, 0.55f, 0.22f), 0.55f);

            // 6 «руны»-точек вокруг внутреннего круга.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + Mathf.PI / 6f;
                var rune = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rune.name = "Rune";
                var col = rune.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                rune.transform.SetParent(arenaRoot, worldPositionStays: false);
                rune.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.15f, Mathf.Sin(a) * 1.15f, 0.013f);
                rune.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Rad2Deg * a + 45f);
                rune.transform.localScale = new Vector3(0.14f, 0.14f, 0.02f);
                rune.GetComponent<MeshRenderer>().sharedMaterial =
                    LitMaterial.GetEmissive(new Color(0.95f, 0.62f, 0.25f), 0.9f);
            }
        }

        private static void BuildWalls(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            var wallColor = new Color(0.25f, 0.20f, 0.22f);
            float wallH = 0.65f;
            float wallT = 0.40f;
            float w = halfWidth * 2f, h = halfHeight * 2f;
            Character3DBuilder.MakeCube(arenaRoot, "WallN", wallColor,
                new Vector3(0f,  halfHeight + wallT * 0.5f, wallH * 0.5f),
                new Vector3(w + wallT * 2f, wallT, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallS", wallColor,
                new Vector3(0f, -halfHeight - wallT * 0.5f, wallH * 0.5f),
                new Vector3(w + wallT * 2f, wallT, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallE", wallColor,
                new Vector3( halfWidth + wallT * 0.5f, 0f, wallH * 0.5f),
                new Vector3(wallT, h, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallW", wallColor,
                new Vector3(-halfWidth - wallT * 0.5f, 0f, wallH * 0.5f),
                new Vector3(wallT, h, wallH));

            // Подсветочные «рейки» вдоль низа стен — тонкие эмиссивные полоски.
            var stripColor = new Color(0.95f, 0.50f, 0.20f);
            Character3DBuilder.MakeCubeEmissive(arenaRoot, "StripN", stripColor, stripColor, 0.55f,
                new Vector3(0f,  halfHeight + 0.02f, 0.08f), new Vector3(w * 0.95f, 0.05f, 0.05f));
            Character3DBuilder.MakeCubeEmissive(arenaRoot, "StripS", stripColor, stripColor, 0.55f,
                new Vector3(0f, -halfHeight - 0.02f, 0.08f), new Vector3(w * 0.95f, 0.05f, 0.05f));
            Character3DBuilder.MakeCubeEmissive(arenaRoot, "StripE", stripColor, stripColor, 0.55f,
                new Vector3( halfWidth + 0.02f, 0f, 0.08f), new Vector3(0.05f, h * 0.95f, 0.05f));
            Character3DBuilder.MakeCubeEmissive(arenaRoot, "StripW", stripColor, stripColor, 0.55f,
                new Vector3(-halfWidth - 0.02f, 0f, 0.08f), new Vector3(0.05f, h * 0.95f, 0.05f));
        }

        private static void BuildDecorativeTrim(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            float w = halfWidth * 2f;
            float h = halfHeight * 2f;
            Color gold = new Color(0.95f, 0.66f, 0.20f);
            Color blood = new Color(0.80f, 0.10f, 0.12f);
            Color blue = new Color(0.25f, 0.70f, 1.0f);

            // Золотые накладки на бортах — дают «дорогой» силуэт как в Hades.
            Character3DBuilder.MakeCube(arenaRoot, "GoldTrimN", gold,
                new Vector3(0f, halfHeight + 0.43f, 0.72f), new Vector3(w * 0.85f, 0.08f, 0.08f));
            Character3DBuilder.MakeCube(arenaRoot, "GoldTrimS", gold,
                new Vector3(0f, -halfHeight - 0.43f, 0.72f), new Vector3(w * 0.85f, 0.08f, 0.08f));
            Character3DBuilder.MakeCube(arenaRoot, "GoldTrimE", gold,
                new Vector3(halfWidth + 0.43f, 0f, 0.72f), new Vector3(0.08f, h * 0.85f, 0.08f));
            Character3DBuilder.MakeCube(arenaRoot, "GoldTrimW", gold,
                new Vector3(-halfWidth - 0.43f, 0f, 0.72f), new Vector3(0.08f, h * 0.85f, 0.08f));

            // Ромбики/акценты на полу по углам: тёплый vs холодный цвет.
            void Diamond(string name, float x, float y, Color c)
            {
                var d = Character3DBuilder.MakeCubeEmissive(arenaRoot, name, c, c, 1.0f,
                    new Vector3(x, y, 0.03f), new Vector3(0.30f, 0.30f, 0.03f));
                d.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            Diamond("FloorSigilNW", -halfWidth * 0.58f, halfHeight * 0.50f, blue);
            Diamond("FloorSigilNE",  halfWidth * 0.58f, halfHeight * 0.50f, gold);
            Diamond("FloorSigilSW", -halfWidth * 0.58f, -halfHeight * 0.50f, blood);
            Diamond("FloorSigilSE",  halfWidth * 0.58f, -halfHeight * 0.50f, blue);
        }

        private static void BuildSideShrines(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            // Маленькие декоративные алтари по бокам: визуальный шум и масштаб сцены.
            void Shrine(string name, float x, float y, Color flame)
            {
                var root = new GameObject(name);
                root.transform.SetParent(arenaRoot, worldPositionStays: false);
                root.transform.localPosition = new Vector3(x, y, 0f);

                Character3DBuilder.MakeCube(root.transform, "Base", new Color(0.18f, 0.14f, 0.16f),
                    new Vector3(0f, 0f, 0.16f), new Vector3(0.55f, 0.45f, 0.32f));
                Character3DBuilder.MakeCube(root.transform, "Top", new Color(0.42f, 0.34f, 0.38f),
                    new Vector3(0f, 0f, 0.45f), new Vector3(0.70f, 0.55f, 0.18f));
                Character3DBuilder.MakeSphereEmissive(root.transform, "Orb", flame, 2.2f,
                    new Vector3(0f, 0f, 0.78f), Vector3.one * 0.28f);
                var lgo = new GameObject("ShrineLight");
                lgo.transform.SetParent(root.transform, worldPositionStays: false);
                lgo.transform.localPosition = new Vector3(0f, 0f, 0.90f);
                var light = lgo.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = flame;
                light.intensity = 1.2f;
                light.range = 3.0f;
                light.shadows = LightShadows.None;
            }

            Shrine("ShrineLeft",  -halfWidth - 1.0f, 0f, new Color(0.35f, 0.85f, 1.0f));
            Shrine("ShrineRight",  halfWidth + 1.0f, 0f, new Color(1.0f, 0.30f, 0.18f));
        }

        private static void BuildColumnsAndTorches(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            var columnColor = new Color(0.32f, 0.28f, 0.32f);
            var capColor    = new Color(0.50f, 0.45f, 0.50f);
            float colR = 0.45f;
            float colH = 2.6f;

            void Column(float x, float y)
            {
                Character3DBuilder.MakeCube(arenaRoot, $"Column_{x:F0}_{y:F0}", columnColor,
                    new Vector3(x, y, colH * 0.5f),
                    new Vector3(colR, colR, colH));
                Character3DBuilder.MakeCube(arenaRoot, $"ColumnCap_{x:F0}_{y:F0}", capColor,
                    new Vector3(x, y, colH),
                    new Vector3(colR * 1.6f, colR * 1.6f, 0.22f));
                // Факел сверху.
                TorchLight.Spawn(arenaRoot,
                    new Vector3(x, y, colH + 0.18f),
                    new Color(1f, 0.55f, 0.18f));
            }

            Column( halfWidth - 0.3f,  halfHeight - 0.3f);
            Column(-halfWidth + 0.3f,  halfHeight - 0.3f);
            Column( halfWidth - 0.3f, -halfHeight + 0.3f);
            Column(-halfWidth + 0.3f, -halfHeight + 0.3f);
        }

        private static void BuildAmbientGlowSpots(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            void GlowSpot(float x, float y, Color c, float s)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "FloorGlow";
                var col = q.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                q.transform.SetParent(arenaRoot, worldPositionStays: false);
                q.transform.localPosition = new Vector3(x, y, 0.018f);
                q.transform.localScale = new Vector3(s, s, 1f);
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = LitMaterial.GetEmissive(c, 0.25f);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            GlowSpot( halfWidth * 0.55f,  halfHeight * 0.55f, new Color(0.95f, 0.45f, 0.12f), 1.8f);
            GlowSpot(-halfWidth * 0.55f, -halfHeight * 0.55f, new Color(0.40f, 0.30f, 0.95f), 1.6f);
        }

        private static void BuildSky(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            // Большой эмиссивный «купол» за ареной — градиент через 4 слоя цветных квадов на разной глубине.
            // Они стоят далеко позади и сбоку, ловятся туманом, дают цвет вместо чёрного фона.
            void SkyPanel(Vector3 localPos, Vector3 localScale, Quaternion rot, Color color, float intensity)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "SkyPanel";
                var col = q.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                q.transform.SetParent(arenaRoot, worldPositionStays: false);
                q.transform.localPosition = localPos;
                q.transform.localRotation = rot;
                q.transform.localScale = localScale;
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = LitMaterial.GetEmissive(color, intensity);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            // Дальняя стена: магента-фиолет.
            SkyPanel(new Vector3(0f, halfHeight + 20f, 8f),
                     new Vector3(60f, 30f, 1f),
                     Quaternion.Euler(90f, 0f, 0f),
                     new Color(0.18f, 0.10f, 0.30f), 0.6f);
            // Нижний слой неба: тёмно-синий.
            SkyPanel(new Vector3(0f, halfHeight + 22f, -2f),
                     new Vector3(70f, 12f, 1f),
                     Quaternion.Euler(90f, 0f, 0f),
                     new Color(0.05f, 0.05f, 0.15f), 0.4f);
            // Левая боковина: магента.
            SkyPanel(new Vector3(-halfWidth - 18f, 4f, 8f),
                     new Vector3(40f, 30f, 1f),
                     Quaternion.Euler(90f, 90f, 0f),
                     new Color(0.30f, 0.10f, 0.40f), 0.5f);
            // Правая боковина: синяя.
            SkyPanel(new Vector3(halfWidth + 18f, 4f, 8f),
                     new Vector3(40f, 30f, 1f),
                     Quaternion.Euler(90f, -90f, 0f),
                     new Color(0.10f, 0.15f, 0.45f), 0.5f);
        }

        public static void BuildLighting()
        {
            var existing = Object.FindObjectsByType<Light>(UnityEngine.FindObjectsSortMode.None);
            foreach (var l in existing)
                if (l != null) Object.Destroy(l.gameObject);

            // Sun (тёплый, яркий, с мягкими тенями).
            var sun = new GameObject("Sun3D");
            var sl = sun.AddComponent<Light>();
            sl.type = LightType.Directional;
            sl.color = new Color(1f, 0.92f, 0.78f);
            sl.intensity = 1.4f;
            sl.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, 35f, 0f);

            // Заполняющий свет — холодный, без теней.
            var fill = new GameObject("Fill3D");
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.55f, 0.70f, 1.0f);
            fl.intensity = 0.55f;
            fl.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(35f, -150f, 0f);

            // Контровый «лунный» свет сверху.
            var rim = new GameObject("Rim3D");
            var rl = rim.AddComponent<Light>();
            rl.type = LightType.Directional;
            rl.color = new Color(0.85f, 0.55f, 1.0f);
            rl.intensity = 0.4f;
            rl.shadows = LightShadows.None;
            rim.transform.rotation = Quaternion.Euler(20f, 180f, 0f);

            // Ambient — тёплый закатный градиент.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.30f, 0.22f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.13f, 0.22f);
            RenderSettings.ambientGroundColor  = new Color(0.08f, 0.06f, 0.10f);

            // Туман — тонкий, для глубины.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.04f, 0.10f);
            RenderSettings.fogDensity = 0.018f;
        }

        public static void ConfigureCamera(Camera cam, float halfWidth, float halfHeight)
        {
            if (cam == null) return;
            // V4: ортографическая 2.5D/isometric камера. Перспектива делала персонажей справа
            // гигантскими роботами и ломала читаемость кадра.
            cam.orthographic = true;
            cam.orthographicSize = 6.6f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.055f, 0.08f);
            cam.transform.position = new Vector3(0f, -8.5f, 7.4f);
            cam.transform.LookAt(new Vector3(0f, 0f, 0.65f), Vector3.up);
        }
    }
}
