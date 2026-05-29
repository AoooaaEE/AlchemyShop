using UnityEngine;

namespace StickEvolve.Core
{
    /// <summary>
    /// Единая палитра игры: дневной стиль "глубокий синий + золото".
    /// Все цвета берём ТОЛЬКО отсюда — никакого хардкода Color в других файлах _StickEvolve.
    /// </summary>
    public static class ColorPalette
    {
        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        // --- Небо / фон ---
        public static readonly Color SkyTop     = Hex("#163E8C"); // насыщенный синий (верх)
        public static readonly Color SkyMid     = Hex("#2E6BC0"); // синий (середина)
        public static readonly Color SkyHorizon = Hex("#F2C14E"); // золотая дымка у горизонта
        public static readonly Color Sun        = Hex("#FFD479"); // солнце
        public static readonly Color SunHalo    = Hex("#FFE9A8"); // гало солнца
        public static readonly Color Cloud      = Hex("#FBEFD0"); // облака (тёплые)

        // --- Рельеф ---
        public static readonly Color MountainFar  = Hex("#3A63A8"); // дальние горы (атм. перспектива)
        public static readonly Color MountainNear = Hex("#1C3B78"); // ближние горы
        public static readonly Color Ground       = Hex("#0E2350"); // земля
        public static readonly Color GroundLine   = Hex("#F2C14E"); // золотая линия пола

        // --- Золото / акценты ---
        public static readonly Color Gold       = Hex("#F2C14E");
        public static readonly Color GoldBright = Hex("#FFD479");
        public static readonly Color GoldDark   = Hex("#B8860B");

        // --- Юниты ---
        public static readonly Color HeroBody    = Hex("#F2C14E"); // свой герой — золотой
        public static readonly Color HeroOutline = Hex("#FFE9A8"); // обводка героя
        public static readonly Color AllyBody    = Hex("#FFD479"); // союзные юниты
        public static readonly Color EnemyBody   = Hex("#C8434B"); // обычный враг (багровый)
        public static readonly Color EnemyElite  = Hex("#42598C"); // элита (сине-стальной)

        // --- Снаряды / эффекты ---
        public static readonly Color BulletAlly  = Hex("#FFD479");
        public static readonly Color BulletEnemy = Hex("#C8434B");
        public static readonly Color GoldDrop    = Hex("#F2C14E");
        public static readonly Color HitFlash    = Hex("#FFFFFF");

        // --- UI / HUD ---
        public static readonly Color HudText   = Hex("#F2C14E");
        public static readonly Color HudPanel  = new Color(0.086f, 0.243f, 0.549f, 0.80f); // #163E8C @ 80%
        public static readonly Color HpBarHigh = Hex("#FFD479");
        public static readonly Color HpBarLow  = Hex("#B8860B");
        public static readonly Color HpBarBack = new Color(0f, 0f, 0f, 0.45f);

        // --- Small decorative / clothing ---
        public static readonly Color EyeColor  = Hex("#07111A");
        public static readonly Color BeltColor = Hex("#2C1B0F");
        public static readonly Color FootColor = Hex("#2C1B0F");
        public static readonly Color HatColor  = Hex("#0A0F1A");
        public static readonly Color CardFrame = Hex("#FFFFFF");
    }
}
