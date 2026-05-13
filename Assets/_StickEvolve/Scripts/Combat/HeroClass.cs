using UnityEngine;

namespace StickEvolve.Combat
{
    public enum HeroClass
    {
        Warrior = 0,
        Archer  = 1,
        Mage    = 2,
        Tank    = 3,
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
            public Color tint;
            public bool hasHat;
            public bool wideShoulders;
            public float bodyScale;
            public string label;
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
                        tint = new Color(0.55f, 0.95f, 0.4f),
                        hasHat = false, wideShoulders = false, bodyScale = 0.95f,
                        label = "Лучник",
                    };
                case HeroClass.Mage:
                    return new Stats
                    {
                        dmgMult = 1.7f, fireRateMult = 0.5f, rangeMult = 1.2f, hpMult = 0.85f,
                        bulletExplosionRadius = 1.4f,
                        tint = new Color(0.75f, 0.5f, 1f),
                        hasHat = true, wideShoulders = false, bodyScale = 0.95f,
                        label = "Маг",
                    };
                case HeroClass.Tank:
                    return new Stats
                    {
                        dmgMult = 0.7f, fireRateMult = 0.85f, rangeMult = 0.8f, hpMult = 2.2f,
                        bulletExplosionRadius = 0f,
                        tint = new Color(1f, 0.55f, 0.3f),
                        hasHat = false, wideShoulders = true, bodyScale = 1.15f,
                        label = "Танк",
                    };
                default:
                    return new Stats
                    {
                        dmgMult = 1f, fireRateMult = 1f, rangeMult = 1f, hpMult = 1f,
                        bulletExplosionRadius = 0f,
                        tint = new Color(0.4f, 0.7f, 1f),
                        hasHat = false, wideShoulders = false, bodyScale = 1f,
                        label = "Воин",
                    };
            }
        }
    }
}
