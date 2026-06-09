using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Строит «3D-болванку» персонажа из стандартных Unity-примитивов:
    /// торс-кубик, голова-сфера, две руки, две ноги, плюс оружие в правой руке.
    /// Геометрия рассчитана на парент, повёрнутый Rx=-90: тогда локальная ось +Z
    /// модели становится мировой осью +Y (вверх). Кубы/сферы симметричны, поэтому
    /// собственная ориентация примитивов не имеет значения.
    /// </summary>
    public static class Character3DBuilder
    {
        public enum Weapon { None, Sword, GreatSword, Dagger, Staff, Bow, Scythe }

        public struct Config
        {
            public Color bodyColor;
            public Color skinColor;
            public Color capeColor;
            public bool hasCape;
            public bool hasHat;
            public bool wideShoulders;
            public float bodyScale;
            public Weapon weapon;
            public bool isHero;
            public string modelResourcePath;
            public float modelScale;
        }

        public static GameObject Build(GameObject root, Config cfg)
        {
            // Контейнер «Visual», куда складываем всю меш-иерархию. Дочерний к root.
            var visual = new GameObject("Visual3D");
            visual.transform.SetParent(root.transform, worldPositionStays: false);

            // V4 style reset: сначала пробуем реальные low-poly GLB модели из Resources.
            // Если ассет не загрузится в Unity, ниже останется старый процедурный fallback,
            // но нормальный путь больше не показывает кубо-роботов.
            if (TryBuildImportedModel(visual.transform, cfg))
            {
                AddGroundGlow(visual.transform, cfg.bodyColor, cfg.isHero);
                visual.AddComponent<Character3DAnimator>();
                return visual;
            }

            // Базовые размеры. Локальная ось Z = «вверх» (после поворота родителя).
            // Чуть увеличиваем только визуал (коллайдеры/геймплей не трогаем), чтобы персонажи читались с 3D-камеры.
            float bs = (cfg.bodyScale <= 0f ? 1f : cfg.bodyScale) * 1.70f;
            float legH = 0.48f * bs;          // высота ноги — короче, чтобы силуэт был chunky, не stickman
            float torsoH = 0.78f * bs;        // высота торса
            float headR = 0.27f * bs;         // крупная голова читается с дальней камеры
            float shoulderW = (cfg.wideShoulders ? 0.72f : 0.56f) * bs;
            float armLen = 0.50f * bs;

            float hipZ = legH;                 // верх ног
            float torsoCenterZ = hipZ + torsoH * 0.5f;
            float shoulderZ = hipZ + torsoH * 0.85f;
            float headCenterZ = hipZ + torsoH + headR;

            // — Ноги —
            float hipHalf = 0.14f * bs;
            MakeCube(visual.transform, "LegL", cfg.bodyColor,
                new Vector3(-hipHalf, 0f, legH * 0.5f),
                new Vector3(0.22f * bs, 0.22f * bs, legH));
            MakeCube(visual.transform, "LegR", cfg.bodyColor,
                new Vector3(hipHalf, 0f, legH * 0.5f),
                new Vector3(0.22f * bs, 0.22f * bs, legH));

            // — Сапоги —
            var bootColor = new Color(0.08f, 0.06f, 0.04f);
            MakeCube(visual.transform, "BootL", bootColor,
                new Vector3(-hipHalf, 0.02f, 0.05f),
                new Vector3(0.30f * bs, 0.34f * bs, 0.12f));
            MakeCube(visual.transform, "BootR", bootColor,
                new Vector3(hipHalf, 0.02f, 0.05f),
                new Vector3(0.30f * bs, 0.34f * bs, 0.12f));

            // — Торс —
            MakeCube(visual.transform, "Torso", cfg.bodyColor,
                new Vector3(0f, 0f, torsoCenterZ),
                new Vector3(shoulderW, 0.42f * bs, torsoH));

            // — Ремень + читаемый class/faction sigil на груди —
            MakeCube(visual.transform, "Belt", new Color(0.18f, 0.12f, 0.06f),
                new Vector3(0f, 0f, hipZ + torsoH * 0.18f),
                new Vector3(shoulderW * 1.08f, 0.46f * bs, 0.12f));
            Color sigil = cfg.isHero ? new Color(0.25f, 0.85f, 1.0f) : new Color(1.0f, 0.22f, 0.14f);
            MakeCubeEmissive(visual.transform, "ChestSigil", sigil, sigil, 1.4f,
                new Vector3(0f, -0.215f * bs, torsoCenterZ + 0.10f * bs),
                new Vector3(0.24f * bs, 0.045f * bs, 0.20f * bs));
            if (cfg.isHero)
            {
                Color capelet = cfg.hasCape ? cfg.capeColor : new Color(0.10f, 0.20f, 0.32f);
                MakeCube(visual.transform, "HeroCapelet", capelet,
                    new Vector3(0f, 0.27f * bs, torsoCenterZ + 0.02f * bs),
                    new Vector3(shoulderW * 0.88f, 0.055f * bs, torsoH * 0.72f));
            }

            // — Плащ (сзади торса по локальной Y) —
            if (cfg.hasCape)
            {
                MakeCube(visual.transform, "Cape", cfg.capeColor,
                    new Vector3(0f, 0.30f * bs, torsoCenterZ - 0.02f * bs),
                    new Vector3(shoulderW * 1.05f, 0.07f * bs, torsoH * 1.00f));
            }

            // — Голова —
            MakeSphere(visual.transform, "Head", cfg.skinColor,
                new Vector3(0f, 0f, headCenterZ),
                Vector3.one * (headR * 2f));

            // — Шлем / рога: чтобы враги и танки читались с камеры даже без текстур —
            if (cfg.hasHat)
            {
                MakeCube(visual.transform, "Helmet", new Color(0.18f, 0.16f, 0.18f),
                    new Vector3(0f, 0f, headCenterZ + headR * 0.55f),
                    new Vector3(headR * 2.2f, headR * 2.0f, headR * 0.55f));
            }
            if (!cfg.isHero)
            {
                Color horn = cfg.hasHat ? new Color(0.75f, 0.55f, 0.28f) : new Color(0.18f, 0.12f, 0.10f);
                var hornL = MakeCube(visual.transform, "HornL", horn,
                    new Vector3(-headR * 0.75f, -headR * 0.10f, headCenterZ + headR * 0.65f),
                    new Vector3(headR * 0.23f, headR * 0.18f, headR * 0.75f));
                hornL.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                var hornR = MakeCube(visual.transform, "HornR", horn,
                    new Vector3(headR * 0.75f, -headR * 0.10f, headCenterZ + headR * 0.65f),
                    new Vector3(headR * 0.23f, headR * 0.18f, headR * 0.75f));
                hornR.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            }

            // — Глаза (эмиссивные точки на передней стороне головы, -Y) —
            Color eyeColor = cfg.isHero ? new Color(0.6f, 0.9f, 1.0f) : new Color(1.0f, 0.25f, 0.20f);
            MakeSphereEmissive(visual.transform, "EyeL", eyeColor, 2.0f,
                new Vector3(-headR * 0.4f, -headR * 0.85f, headCenterZ + headR * 0.10f),
                Vector3.one * (headR * 0.42f));
            MakeSphereEmissive(visual.transform, "EyeR", eyeColor, 2.0f,
                new Vector3(headR * 0.4f, -headR * 0.85f, headCenterZ + headR * 0.10f),
                Vector3.one * (headR * 0.42f));

            // — Руки —
            float shoulderHalf = shoulderW * 0.5f + 0.05f;

            // — Плечевые накладки (немного ярче торса) —
            Color pauldron = new Color(
                Mathf.Clamp01(cfg.bodyColor.r * 1.3f + 0.05f),
                Mathf.Clamp01(cfg.bodyColor.g * 1.3f + 0.05f),
                Mathf.Clamp01(cfg.bodyColor.b * 1.3f + 0.05f));
            MakeCube(visual.transform, "PauldronL", pauldron,
                new Vector3(-shoulderHalf, 0f, shoulderZ + 0.05f),
                new Vector3(0.34f * bs, 0.30f * bs, 0.18f));
            MakeCube(visual.transform, "PauldronR", pauldron,
                new Vector3(shoulderHalf, 0f, shoulderZ + 0.05f),
                new Vector3(0.34f * bs, 0.30f * bs, 0.18f));
            // Левая рука — опущена.
            MakeCube(visual.transform, "ArmL", cfg.bodyColor,
                new Vector3(-shoulderHalf, 0f, shoulderZ - armLen * 0.5f),
                new Vector3(0.22f * bs, 0.22f * bs, armLen));
            // Правая рука — слегка вперёд, держит оружие.
            var armR = MakeCube(visual.transform, "ArmR", cfg.bodyColor,
                new Vector3(shoulderHalf, -0.15f * bs, shoulderZ - armLen * 0.4f),
                new Vector3(0.22f * bs, 0.22f * bs, armLen));

            // — Оружие в правой руке —
            if (cfg.weapon != Weapon.None)
            {
                var weapon = new GameObject("Weapon");
                weapon.transform.SetParent(visual.transform, worldPositionStays: false);
                // У правой руки кисть находится примерно тут:
                Vector3 handPos = new Vector3(shoulderHalf, -0.30f * bs, shoulderZ - armLen);
                weapon.transform.localPosition = handPos;
                weapon.transform.localScale = Vector3.one * Mathf.Clamp(bs * 0.78f, 0.95f, 1.45f);
                BuildWeapon(weapon.transform, cfg.weapon, cfg.bodyColor);
            }

            // — Светящееся кольцо под ногами (декаль на полу: тонкий плоский цилиндр) —
            AddGroundGlow(visual.transform, cfg.bodyColor, cfg.isHero);

            // Code-only procedural animation: walk/idle/attack/readability without Animator Controller.
            visual.AddComponent<Character3DAnimator>();

            return visual;
        }

        private static bool TryBuildImportedModel(Transform visual, Config cfg)
        {
            if (string.IsNullOrEmpty(cfg.modelResourcePath)) return false;
            var prefab = Resources.Load<GameObject>(cfg.modelResourcePath);
            if (prefab == null) return false;

            var model = Object.Instantiate(prefab, visual, false);
            model.name = "ImportedModel";
            // GLB-модели стандартно Y-up, а наш 3D-геймплей живёт в XY, высота = Z.
            // Rx=90 переводит локальную высоту модели в Z. Доп. разворот нужен, чтобы лицо смотрело в -Y.
            model.transform.localRotation = Quaternion.Euler(90f, 0f, cfg.isHero ? 180f : 0f);
            float s = cfg.modelScale > 0f ? cfg.modelScale : 1f;
            s *= Mathf.Clamp(cfg.bodyScale <= 0f ? 1f : cfg.bodyScale, 0.65f, 1.55f);
            model.transform.localScale = Vector3.one * s;
            model.transform.localPosition = Vector3.zero;

            // Убираем случайные коллайдеры с импортированных ассетов: физика остаётся на root CapsuleCollider2D.
            var colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) Object.Destroy(colliders[i]);

            // Командный маркер читаемости без превращения модели в робота.
            Color marker = cfg.isHero ? new Color(0.20f, 0.85f, 1.0f) : new Color(1.0f, 0.20f, 0.12f);
            MakeCubeEmissive(visual, cfg.isHero ? "HeroTeamMarker" : "EnemyTeamMarker", marker, marker, cfg.isHero ? 0.55f : 0.75f,
                new Vector3(0f, -0.22f, 1.15f), new Vector3(0.28f, 0.035f, 0.10f));

            // Небольшое оружие/магический предмет оставляем как gameplay-readability, но уже не как тело персонажа.
            if (cfg.weapon != Weapon.None)
            {
                var weapon = new GameObject("Weapon");
                weapon.transform.SetParent(visual, worldPositionStays: false);
                weapon.transform.localPosition = new Vector3(0.34f, -0.36f, 0.82f);
                weapon.transform.localRotation = Quaternion.Euler(12f, 0f, -18f);
                weapon.transform.localScale = Vector3.one * 0.72f;
                BuildWeapon(weapon.transform, cfg.weapon, cfg.bodyColor);
            }
            return true;
        }

        private static void BuildWeapon(Transform parent, Weapon w, Color tint)
        {
            Color bladeEmissive = new Color(0.55f, 0.75f, 1.0f);    // холодный «энчант»
            Color glowEmissive  = new Color(
                Mathf.Min(1f, tint.r + 0.5f),
                Mathf.Min(1f, tint.g + 0.4f),
                Mathf.Min(1f, tint.b + 0.6f));
            switch (w)
            {
                case Weapon.Sword:
                    MakeCube(parent, "Hilt",  new Color(0.32f, 0.22f, 0.12f), new Vector3(0f, 0f, -0.10f), new Vector3(0.08f, 0.08f, 0.18f));
                    MakeCube(parent, "Guard", new Color(0.85f, 0.62f, 0.25f), new Vector3(0f, 0f, 0f),    new Vector3(0.30f, 0.08f, 0.06f));
                    MakeCubeEmissive(parent, "Blade", new Color(0.92f, 0.96f, 1.0f), bladeEmissive, 0.7f, new Vector3(0f, 0f, 0.40f), new Vector3(0.10f, 0.04f, 0.70f));
                    break;
                case Weapon.GreatSword:
                    MakeCube(parent, "Hilt",  new Color(0.30f, 0.20f, 0.10f), new Vector3(0f, 0f, -0.18f), new Vector3(0.10f, 0.10f, 0.28f));
                    MakeCube(parent, "Guard", new Color(0.75f, 0.55f, 0.20f), new Vector3(0f, 0f, 0f),     new Vector3(0.45f, 0.10f, 0.07f));
                    MakeCubeEmissive(parent, "Blade", new Color(0.94f, 0.96f, 1.0f), bladeEmissive, 0.7f, new Vector3(0f, 0f, 0.55f), new Vector3(0.16f, 0.05f, 1.00f));
                    break;
                case Weapon.Dagger:
                    MakeCube(parent, "Hilt",  new Color(0.18f, 0.12f, 0.08f), new Vector3(0f, 0f, -0.05f), new Vector3(0.07f, 0.07f, 0.12f));
                    MakeCubeEmissive(parent, "Blade", new Color(0.90f, 0.94f, 1.0f), bladeEmissive, 0.6f, new Vector3(0f, 0f, 0.18f), new Vector3(0.07f, 0.03f, 0.30f));
                    break;
                case Weapon.Staff:
                    MakeCube(parent, "Shaft", new Color(0.40f, 0.25f, 0.12f), new Vector3(0f, 0f, 0.30f), new Vector3(0.06f, 0.06f, 0.95f));
                    MakeSphereEmissive(parent, "OrbGlow", glowEmissive, 0.8f, new Vector3(0f, 0f, 0.85f), Vector3.one * 0.45f);
                    MakeSphereEmissive(parent, "Orb",     glowEmissive, 2.5f, new Vector3(0f, 0f, 0.85f), Vector3.one * 0.22f);
                    break;
                case Weapon.Bow:
                    MakeCube(parent, "BowTop", new Color(0.55f, 0.32f, 0.16f), new Vector3(0f, 0f, 0.25f), new Vector3(0.06f, 0.06f, 0.45f));
                    MakeCube(parent, "BowBot", new Color(0.55f, 0.32f, 0.16f), new Vector3(0f, 0f, -0.25f), new Vector3(0.06f, 0.06f, 0.45f));
                    MakeCubeEmissive(parent, "String", new Color(0.95f, 0.95f, 0.95f), bladeEmissive, 0.3f, new Vector3(-0.12f, 0f, 0f), new Vector3(0.02f, 0.02f, 0.85f));
                    break;
                case Weapon.Scythe:
                    MakeCube(parent, "Shaft", new Color(0.22f, 0.10f, 0.05f), new Vector3(0f, 0f, 0.30f), new Vector3(0.07f, 0.07f, 1.05f));
                    var blade = MakeCubeEmissive(parent, "Blade", new Color(0.86f, 0.88f, 0.94f), new Color(0.6f, 0.20f, 0.85f), 0.8f, new Vector3(0.25f, 0f, 0.82f), new Vector3(0.50f, 0.04f, 0.10f));
                    blade.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
                    break;
            }
        }

        private static void AddGroundGlow(Transform parent, Color tint, bool isHero)
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "GroundGlow";
            // Сносим коллайдер от примитива.
            var col = glow.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            glow.transform.SetParent(parent, worldPositionStays: false);
            // Quad по умолчанию в плоскости XY локальной модели; поворачиваем чтобы лёг плашмя.
            // Локальный +Z = мировой +Y (вверх). Хотим лежащий на полу диск.
            glow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // плоскость XY = лежит как нужно
            glow.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            glow.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            var mr = glow.GetComponent<MeshRenderer>();
            var mat = LitMaterial.Get(isHero
                ? new Color(Mathf.Min(1f, tint.r + 0.35f), Mathf.Min(1f, tint.g + 0.25f), Mathf.Min(1f, tint.b + 0.05f), 0.32f)
                : new Color(0.85f, 0.10f, 0.15f, 0.28f), transparent: true);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        // === HELPERS ===

        public static GameObject MakeCubeEmissive(Transform parent, string name, Color baseColor, Color emitColor, float emitIntensity, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            var mat = new Material(LitMaterial.SharedShader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            var emit = emitColor * emitIntensity;
            emit.a = 1f;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emit);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mr.sharedMaterial = mat;
            return go;
        }

        public static GameObject MakeSphereEmissive(Transform parent, string name, Color emitColor, float emitIntensity, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LitMaterial.GetEmissive(emitColor, emitIntensity);
            return go;
        }

        public static GameObject MakeCube(Transform parent, string name, Color color, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LitMaterial.Get(color);
            return go;
        }

        public static GameObject MakeSphere(Transform parent, string name, Color color, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LitMaterial.Get(color);
            return go;
        }
    }

    /// <summary>
    /// Кэш URP/Lit-материалов по цвету. Запасной шейдер — Standard/Sprites/Default чтобы не было розового на любых пайплайнах.
    /// </summary>
    internal static class LitMaterial
    {
        private static System.Collections.Generic.Dictionary<int, Material> _cache = new();
        private static Shader _shader;

        public static Shader SharedShader => GetShader();

        private static Shader GetShader()
        {
            if (_shader != null) return _shader;
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("Standard");
            if (_shader == null) _shader = Shader.Find("Sprites/Default");
            return _shader;
        }

        public static Material GetEmissive(Color color, float intensity)
        {
            int key = ColorKey(color, tr: false) ^ 0x7FFF0000 ^ Mathf.RoundToInt(intensity * 100f);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            var mat = new Material(GetShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.0f);
            var emit = color * intensity;
            emit.a = 1f;
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emit);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _cache[key] = mat;
            return mat;
        }

        public static Material Get(Color color, bool transparent = false)
        {
            int key = ColorKey(color, transparent);
            if (_cache.TryGetValue(key, out var m) && m != null) return m;
            var mat = new Material(GetShader());
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.32f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.02f);
            if (transparent)
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);    // URP transparent
                if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 0f);      // alpha blend
                if (mat.HasProperty("_ZWrite"))  mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            _cache[key] = mat;
            return mat;
        }

        private static int ColorKey(Color c, bool tr) =>
            ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | ((int)(c.a * 255)) | (tr ? 1 : 0);
    }
}
