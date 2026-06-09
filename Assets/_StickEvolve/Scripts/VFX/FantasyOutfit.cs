using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Что декорировать поверх стикмена: оружие в руке + светящееся кольцо под ногами.
    /// Позволяет одной строкой из спавнера сделать юнита «фентезийным».
    /// </summary>
    public static class FantasyOutfit
    {
        public enum Weapon { None, Sword, Staff, Bow, Dagger, GreatSword, Scythe }

        public static void Apply(GameObject root, Color tint, Weapon weapon, bool isHero)
        {
            if (root == null) return;
            AddGlowRing(root, tint, isHero);
            AddWeapon(root, tint, weapon);
        }

        // === GLOW RING ===

        private static void AddGlowRing(GameObject root, Color tint, bool isHero)
        {
            var go = new GameObject("GlowRing");
            go.transform.SetParent(root.transform, worldPositionStays: false);
            // Чуть ниже центра, под ногами.
            go.transform.localPosition = new Vector3(0f, -0.78f, 0.02f);
            go.transform.localScale = new Vector3(1.05f, 0.42f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ParticleSprite.Get(); // мягкий радиальный градиент
            var c = isHero
                ? new Color(Mathf.Min(1f, tint.r * 1.2f + 0.3f), Mathf.Min(1f, tint.g * 1.2f + 0.25f), Mathf.Min(1f, tint.b * 1.2f + 0.15f), 0.55f)
                : new Color(Mathf.Min(1f, tint.r * 1.2f + 0.2f), tint.g * 0.5f, tint.b * 0.5f, 0.55f);
            sr.color = c;
            sr.sortingOrder = 0; // под телом (тело >=2)
        }

        // === WEAPON ===

        private static void AddWeapon(GameObject root, Color tint, Weapon weapon)
        {
            if (weapon == Weapon.None) return;
            var holder = new GameObject("Weapon");
            holder.transform.SetParent(root.transform, worldPositionStays: false);
            // Оружие справа-впереди от тела, чтобы было видно на top-down.
            holder.transform.localPosition = new Vector3(0.34f, 0.08f, -0.05f);
            holder.transform.localRotation = Quaternion.Euler(0f, 0f, -22f);

            switch (weapon)
            {
                case Weapon.Sword:      BuildSword(holder.transform, 0.55f, new Color(0.85f, 0.88f, 0.95f), new Color(0.40f, 0.30f, 0.18f), guard: 0.18f); break;
                case Weapon.GreatSword: BuildSword(holder.transform, 0.85f, new Color(0.90f, 0.92f, 0.98f), new Color(0.30f, 0.20f, 0.12f), guard: 0.28f, bladeThick: 0.12f); break;
                case Weapon.Dagger:     BuildSword(holder.transform, 0.30f, new Color(0.80f, 0.82f, 0.90f), new Color(0.18f, 0.12f, 0.08f), guard: 0.12f, bladeThick: 0.06f); break;
                case Weapon.Staff:      BuildStaff(holder.transform, tint); break;
                case Weapon.Bow:        BuildBow(holder.transform); break;
                case Weapon.Scythe:     BuildScythe(holder.transform); break;
            }
        }

        private static void BuildSword(Transform parent, float bladeLen, Color bladeColor, Color hiltColor, float guard, float bladeThick = 0.08f)
        {
            // Рукоять
            MakeRect(parent, "Hilt", hiltColor, new Vector3(0f, -0.10f, 0f), new Vector3(0.06f, 0.18f, 1f), sortingOrder: 8);
            // Гарда
            MakeRect(parent, "Guard", new Color(0.55f, 0.42f, 0.20f), new Vector3(0f, 0f, 0f), new Vector3(guard, 0.06f, 1f), sortingOrder: 9);
            // Лезвие
            MakeRect(parent, "Blade", bladeColor, new Vector3(0f, bladeLen * 0.5f + 0.02f, 0f), new Vector3(bladeThick, bladeLen, 1f), sortingOrder: 8);
            // Светлая полоска по центру лезвия
            MakeRect(parent, "BladeShine", new Color(1f, 1f, 1f, 0.7f), new Vector3(0f, bladeLen * 0.5f + 0.02f, -0.001f), new Vector3(bladeThick * 0.25f, bladeLen * 0.9f, 1f), sortingOrder: 9);
        }

        private static void BuildStaff(Transform parent, Color tint)
        {
            // Древко
            MakeRect(parent, "Shaft", new Color(0.35f, 0.22f, 0.12f), new Vector3(0f, 0.25f, 0f), new Vector3(0.05f, 0.75f, 1f), sortingOrder: 8);
            // Орб наверху
            var orbColor = new Color(Mathf.Min(1f, tint.r + 0.25f), Mathf.Min(1f, tint.g + 0.25f), Mathf.Min(1f, tint.b + 0.3f), 1f);
            MakeCircle(parent, "OrbGlow", new Color(orbColor.r, orbColor.g, orbColor.b, 0.45f), new Vector3(0f, 0.72f, 0.01f), Vector3.one * 0.40f, sortingOrder: 9);
            MakeCircle(parent, "Orb",     orbColor,                                                     new Vector3(0f, 0.72f, 0f),    Vector3.one * 0.22f, sortingOrder: 10);
            MakeCircle(parent, "OrbCore", new Color(1f, 1f, 1f, 0.85f),                                 new Vector3(0f, 0.74f, -0.001f), Vector3.one * 0.08f, sortingOrder: 11);
        }

        private static void BuildBow(Transform parent)
        {
            var wood = new Color(0.45f, 0.28f, 0.15f);
            // Лук: две дуги — две вытянутые тонкие полоски, расположенные V-образно.
            var top = MakeRect(parent, "BowTop", wood, new Vector3(0.02f, 0.22f, 0f), new Vector3(0.06f, 0.40f, 1f), sortingOrder: 8);
            top.localRotation = Quaternion.Euler(0f, 0f, -18f);
            var bot = MakeRect(parent, "BowBot", wood, new Vector3(0.02f, -0.22f, 0f), new Vector3(0.06f, 0.40f, 1f), sortingOrder: 8);
            bot.localRotation = Quaternion.Euler(0f, 0f, 18f);
            // Тетива
            MakeRect(parent, "BowString", new Color(0.95f, 0.95f, 0.95f, 0.85f), new Vector3(-0.10f, 0f, -0.001f), new Vector3(0.015f, 0.78f, 1f), sortingOrder: 9);
        }

        private static void BuildScythe(Transform parent)
        {
            // Древко
            MakeRect(parent, "ScytheShaft", new Color(0.18f, 0.10f, 0.06f), new Vector3(0f, 0.20f, 0f), new Vector3(0.05f, 0.85f, 1f), sortingOrder: 8);
            // Лезвие — наклонная полоска сверху
            var blade = MakeRect(parent, "ScytheBlade", new Color(0.85f, 0.85f, 0.95f), new Vector3(0.18f, 0.62f, 0f), new Vector3(0.36f, 0.07f, 1f), sortingOrder: 9);
            blade.localRotation = Quaternion.Euler(0f, 0f, -35f);
        }

        // === HELPERS ===

        private static Transform MakeRect(Transform parent, string name, Color color, Vector3 localPos, Vector3 localScale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SquareSprite.Get();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        private static Transform MakeCircle(Transform parent, string name, Color color, Vector3 localPos, Vector3 localScale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ParticleSprite.Get();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }
    }

    /// <summary>Ленивая 1×1 белая квадратная текстура, чтобы не зависеть от SpriteFactory.</summary>
    internal static class SquareSprite
    {
        private static Sprite _s;
        public static Sprite Get()
        {
            if (_s != null) return _s;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) t.SetPixel(x, y, Color.white);
            t.Apply();
            _s = Sprite.Create(t, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
            _s.name = "WhiteSquare";
            return _s;
        }
    }
}
