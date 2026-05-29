using UnityEngine;
using StickEvolve.Core;

namespace StickEvolve.Combat
{
    public enum HeroClass
    {
        Warrior   = 0,
        Archer    = 1,
        Mage      = 2,
        Tank      = 3,
        Healer    = 4,
        Berserker = 5,
        Sniper    = 6,
        Ninja     = 7,
    }

    /// <summary>
    /// Множители и визуал классов героев. Накладываются поверх HeroDefaults
    /// (которые приходят из накопленных карт).
    /// </summary>
    public static class HeroClassStats
    {
        public struct Stats
        {
            public float dmgMult;
            public float fireRateMult;
            public float rangeMult;
            public float hpMult;
            public float bulletExplosionRadius;
            public float critChanceBonus;
            public float critMultBonus;
            public Color tint;
            public bool hasHat;
            public bool wideShoulders;
            public float bodyScale;
            public string label;
            public bool isHealer;            // стреляет в раненых союзников
            public float berserkerScale;     // 0..N — насколько урон растёт по мере падения HP
            public float ninjaCloneChance;   // 0..1 — шанс при крите спавнить клона
            public bool hasCape;
            public Color capeColor;
        }

        public static Stats Get(HeroClass cls)
        {
            switch (cls)
            {
                case HeroClass.Archer:
                    return new Stats
                    {
                        dmgMult = 0.9f, fireRateMult = 0.85f, rangeMult = 1.6f, hpMult = 0.75f,
                        bulletExplosionRadius = 0f,
                        tint = ColorPalette.Gold,
                        hasHat = false, wideShoulders = false, bodyScale = 0.95f,
                        label = "Лучник",
                    };
                case HeroClass.Mage:
                    return new Stats
                    {
                        dmgMult = 1.7f, fireRateMult = 0.5f, rangeMult = 1.2f, hpMult = 0.85f,
                        bulletExplosionRadius = 1.4f,
                        tint = ColorPalette.Sun,
                        hasHat = true, wideShoulders = false, bodyScale = 0.95f,
                        label = "Маг",
                        hasCape = true, capeColor = ColorPalette.EnemyElite,
                    };
                case HeroClass.Tank:
                    return new Stats
                    {
                        dmgMult = 0.7f, fireRateMult = 0.85f, rangeMult = 0.8f, hpMult = 2.2f,
                        bulletExplosionRadius = 0f,
                        tint = ColorPalette.HudPanel,
                        hasHat = false, wideShoulders = true, bodyScale = 1.15f,
                        label = "Танк",
                    };
                case HeroClass.Healer:
                    return new Stats
                    {
                        dmgMult = 0.6f, fireRateMult = 0.9f, rangeMult = 1.3f, hpMult = 1f,
                        bulletExplosionRadius = 0f,
                        tint = ColorPalette.GoldBright,
                        hasHat = true, wideShoulders = false, bodyScale = 0.95f,
                        label = "Жрец",
                        isHealer = true,
                        hasCape = true, capeColor = ColorPalette.HitFlash,
                    };
                case HeroClass.Berserker:
                    return new Stats
                    {
                        dmgMult = 1.2f, fireRateMult = 1.4f, rangeMult = 0.85f, hpMult = 1.3f,
                        bulletExplosionRadius = 0f,
                        tint = ColorPalette.EnemyBody,
                        hasHat = false, wideShoulders = true, bodyScale = 1.05f,
                        label = "Берсерк",
                        berserkerScale = 1.6f,
                    };
                case HeroClass.Sniper:
                    return new Stats
                    {
                        dmgMult = 2.5f, fireRateMult = 0.4f, rangeMult = 2.2f, hpMult = 0.7f,
                        bulletExplosionRadius = 0f,
                        critChanceBonus = 0.20f,
                        critMultBonus = 1.0f,
                        tint = ColorPalette.MountainFar,
                        hasHat = true, wideShoulders = false, bodyScale = 0.95f,
                        label = "Снайпер",
                    };
                case HeroClass.Ninja:
                    return new Stats
                    {
                        dmgMult = 1.1f, fireRateMult = 1.8f, rangeMult = 1f, hpMult = 0.8f,
                        bulletExplosionRadius = 0f,
                        critChanceBonus = 0.15f,
                        tint = ColorPalette.HpBarBack,
                        hasHat = false, wideShoulders = false, bodyScale = 0.95f,
                        label = "Ниндзя",
                        ninjaCloneChance = 0.6f,
                        hasCape = true, capeColor = ColorPalette.HpBarBack,
                    };
                default:
                    return new Stats
                    {
                        dmgMult = 1f, fireRateMult = 1f, rangeMult = 1f, hpMult = 1f,
                        bulletExplosionRadius = 0f,
                        tint = ColorPalette.BulletAlly,
                        hasHat = false, wideShoulders = false, bodyScale = 1f,
                        label = "Воин",
                    };
            }
        }
    }
}
