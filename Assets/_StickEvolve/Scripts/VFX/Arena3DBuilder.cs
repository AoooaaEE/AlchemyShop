using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Строит 3D-арену: пол (плита), 4 стены по краям, свет + ambient, fog.
    /// Все элементы — дочерние arenaRoot, который ожидается повёрнутым на Rx=-90,
    /// чтобы локальная плоскость XY = мировой пол XZ, а локальная +Z = мировой +Y.
    /// </summary>
    public static class Arena3DBuilder
    {
        public static void Build(Transform arenaRoot, float halfWidth, float halfHeight)
        {
            // — Пол (большой Quad в локальной XY-плоскости) —
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = "Floor3D";
            var fc = floor.GetComponent<Collider>(); if (fc != null) Object.Destroy(fc);
            floor.transform.SetParent(arenaRoot, worldPositionStays: false);
            floor.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            floor.transform.localScale = new Vector3(halfWidth * 2.4f, halfHeight * 2.4f, 1f);
            var floorMat = LitMaterial.Get(new Color(0.12f, 0.10f, 0.14f));
            var floorMR = floor.GetComponent<MeshRenderer>();
            floorMR.sharedMaterial = floorMat;
            floorMR.receiveShadows = true;

            // — Контур арены: 4 низкие стены —
            var wallColor = new Color(0.22f, 0.18f, 0.20f);
            float wallH = 0.6f;
            float wallT = 0.4f;
            float w = halfWidth * 2f, h = halfHeight * 2f;
            // Север (локальная +Y)
            Character3DBuilder.MakeCube(arenaRoot, "WallN", wallColor,
                new Vector3(0f, halfHeight + wallT * 0.5f, wallH * 0.5f),
                new Vector3(w + wallT * 2f, wallT, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallS", wallColor,
                new Vector3(0f, -halfHeight - wallT * 0.5f, wallH * 0.5f),
                new Vector3(w + wallT * 2f, wallT, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallE", wallColor,
                new Vector3(halfWidth + wallT * 0.5f, 0f, wallH * 0.5f),
                new Vector3(wallT, h, wallH));
            Character3DBuilder.MakeCube(arenaRoot, "WallW", wallColor,
                new Vector3(-halfWidth - wallT * 0.5f, 0f, wallH * 0.5f),
                new Vector3(wallT, h, wallH));

            // — Колонны в 4 углах —
            var columnColor = new Color(0.30f, 0.26f, 0.30f);
            float colR = 0.45f;
            float colH = 2.5f;
            void Column(float x, float y) {
                Character3DBuilder.MakeCube(arenaRoot, $"Column_{x:F0}_{y:F0}", columnColor,
                    new Vector3(x, y, colH * 0.5f),
                    new Vector3(colR, colR, colH));
                Character3DBuilder.MakeCube(arenaRoot, $"ColumnCap_{x:F0}_{y:F0}", new Color(0.42f, 0.38f, 0.42f),
                    new Vector3(x, y, colH),
                    new Vector3(colR * 1.4f, colR * 1.4f, 0.20f));
            }
            Column( halfWidth - 0.3f,  halfHeight - 0.3f);
            Column(-halfWidth + 0.3f,  halfHeight - 0.3f);
            Column( halfWidth - 0.3f, -halfHeight + 0.3f);
            Column(-halfWidth + 0.3f, -halfHeight + 0.3f);

            // — Лужи свечения на полу для атмосферы (4 точки) —
            void GlowSpot(float x, float y, Color c, float s) {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "FloorGlow";
                var col = q.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                q.transform.SetParent(arenaRoot, worldPositionStays: false);
                q.transform.localPosition = new Vector3(x, y, 0.01f);
                q.transform.localScale = new Vector3(s, s, 1f);
                var mr = q.GetComponent<MeshRenderer>();
                mr.sharedMaterial = LitMaterial.Get(c, transparent: true);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
            GlowSpot( halfWidth * 0.6f,  halfHeight * 0.6f, new Color(0.95f, 0.45f, 0.10f, 0.35f), 2.2f);
            GlowSpot(-halfWidth * 0.6f,  halfHeight * 0.6f, new Color(0.95f, 0.45f, 0.10f, 0.35f), 2.2f);
            GlowSpot( halfWidth * 0.6f, -halfHeight * 0.6f, new Color(0.30f, 0.20f, 0.85f, 0.30f), 2.0f);
            GlowSpot(-halfWidth * 0.6f, -halfHeight * 0.6f, new Color(0.30f, 0.20f, 0.85f, 0.30f), 2.0f);
        }

        public static void BuildLighting()
        {
            // Удаляем дефолтные источники света на сцене.
            var existing = Object.FindObjectsByType<Light>(UnityEngine.FindObjectsSortMode.None);
            foreach (var l in existing)
                if (l != null) Object.Destroy(l.gameObject);

            // Основной направленный свет (как «солнце»).
            var sun = new GameObject("Sun3D");
            var sl = sun.AddComponent<Light>();
            sl.type = LightType.Directional;
            sl.color = new Color(1f, 0.94f, 0.82f);
            sl.intensity = 1.1f;
            sl.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(55f, 35f, 0f);

            // Заполняющий свет, чуть холоднее, с противоположной стороны.
            var fill = new GameObject("Fill3D");
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.color = new Color(0.55f, 0.62f, 0.95f);
            fl.intensity = 0.35f;
            fl.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(35f, -150f, 0f);

            // Глобальный ambient.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = new Color(0.18f, 0.16f, 0.22f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.10f, 0.14f);
            RenderSettings.ambientGroundColor  = new Color(0.06f, 0.05f, 0.08f);

            // Лёгкий туман для глубины.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.08f);
            RenderSettings.fogDensity = 0.025f;
        }

        public static void ConfigureCamera(Camera cam, float halfWidth, float halfHeight)
        {
            if (cam == null) return;
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
            // Геймплей — на мировой XY-плоскости (z=0). Высота персонажей вдоль +Z.
            // Камера: сзади-снизу-сверху, смотрит в центр арены. Расположение подобрано так,
            // чтобы вертикальная (портретная) рамка покрывала всю арену с небольшим запасом.
            cam.transform.position = new Vector3(0f, -11f, 9f);
            cam.transform.LookAt(new Vector3(0f, 1f, 0.8f), Vector3.up);
        }
    }
}
