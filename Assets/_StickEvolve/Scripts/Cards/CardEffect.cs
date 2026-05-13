using System;
using StickEvolve.Combat;
using UnityEngine;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Применение карты:
    /// 1) обновить CardProgression (уровень + pity).
    /// 2) сделать одноразовые эффекты (FullHeal, SpawnExtraHero, SpawnHeroOfClass).
    /// 3) пересчитать накопленные статы и применить их ко всем живым героям.
    /// </summary>
    public static class CardEffect
    {
        public delegate Hero HeroFactory();
        public delegate Hero HeroFactoryByClass(HeroClass cls);

        public static HeroFactory ExtraHeroSpawner;
        public static HeroFactoryByClass ExtraHeroSpawnerByClass;

        public static void Apply(CardSO card)
        {
            if (card == null) return;

            CardProgression.RegisterPick(card);

            if (card.effect == CardEffectKind.FullHeal)
            {
                FullHealAlive();
            }
            else if (card.effect == CardEffectKind.SpawnExtraHero)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(card.value));
                for (int i = 0; i < n; i++) ExtraHeroSpawner?.Invoke();
            }
            else if (card.effect == CardEffectKind.SpawnHeroOfClass)
            {
                int classInt = Mathf.RoundToInt(card.secondaryValue);
                var cls = (HeroClass)Mathf.Clamp(classInt, 0, Enum.GetValues(typeof(HeroClass)).Length - 1);
                int n = Mathf.Max(1, Mathf.RoundToInt(card.value));
                for (int i = 0; i < n; i++) ExtraHeroSpawnerByClass?.Invoke(cls);
            }

            ApplyDefaultsToAllAlive();
        }

        public static void ApplyDefaultsToAllAlive()
        {
            var d = CardProgression.Compute();
            var heroes = HeroRegistry.Instance.Alive;
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                if (h == null) continue;
                ApplyDefaultsTo(h, d, fullHeal: false);
            }
        }

        public static void ApplyDefaultsToNewHero(Hero hero)
        {
            if (hero == null) return;
            var d = CardProgression.Compute();
            ApplyDefaultsTo(hero, d, fullHeal: true);
        }

        private static void ApplyDefaultsTo(Hero h, HeroDefaults d, bool fullHeal)
        {
            var s = HeroClassStats.Get(h.HeroClass);
            h.damage = d.damage * s.dmgMult;
            h.fireRate = d.fireRate * s.fireRateMult;
            h.range = d.range * s.rangeMult;
            h.bulletSpeed = d.bulletSpeed;
            h.critChance = Mathf.Clamp01(d.critChance + s.critChanceBonus);
            h.critMultiplier = d.critMultiplier + s.critMultBonus;
            h.bulletExplosionRadius = s.bulletExplosionRadius;
            h.isHealer = s.isHealer;
            h.berserkerScale = s.berserkerScale;
            h.ninjaCloneChance = s.ninjaCloneChance;
            h.multiShot = d.multiShot;
            h.bulletPierce = d.bulletPierce;
            h.lifesteal = d.lifesteal;
            h.thornsDamage = d.thorns;
            var hp = h.GetComponent<Health>();
            if (hp != null) hp.Configure(d.maxHp * s.hpMult, fullHeal);
        }

        private static void FullHealAlive()
        {
            var heroes = HeroRegistry.Instance.Alive;
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                if (h == null) continue;
                var hp = h.GetComponent<Health>();
                if (hp != null) hp.Heal(hp.MaxHp);
            }
        }
    }
}
