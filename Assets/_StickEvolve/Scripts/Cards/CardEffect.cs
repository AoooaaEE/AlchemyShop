using StickEvolve.Combat;
using UnityEngine;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Применяет эффект карты к существующим героям. PrototypeBootstrapper передаёт сюда
    /// фабрику для спавна нового героя при эффекте SpawnExtraHero.
    /// </summary>
    public static class CardEffect
    {
        public delegate Hero HeroFactory();
        public static HeroFactory ExtraHeroSpawner;

        public static void Apply(CardSO card)
        {
            if (card == null) return;
            var heroes = HeroRegistry.Instance.Alive;

            switch (card.effect)
            {
                case CardEffectKind.DamageMultiplier:
                    foreach (var h in heroes) if (h != null) h.damage *= card.value;
                    break;
                case CardEffectKind.FireRateMultiplier:
                    foreach (var h in heroes) if (h != null) h.fireRate *= card.value;
                    break;
                case CardEffectKind.RangeAdd:
                    foreach (var h in heroes) if (h != null) h.range += card.value;
                    break;
                case CardEffectKind.MaxHpMultiplier:
                    foreach (var h in heroes)
                    {
                        if (h == null) continue;
                        var hp = h.GetComponent<Health>();
                        if (hp == null) continue;
                        hp.Configure(hp.MaxHp * card.value, fullHeal: true);
                    }
                    break;
                case CardEffectKind.CritChanceAdd:
                    foreach (var h in heroes) if (h != null) h.critChance = Mathf.Clamp01(h.critChance + card.value);
                    break;
                case CardEffectKind.CritMultiplierAdd:
                    foreach (var h in heroes) if (h != null) h.critMultiplier += card.value;
                    break;
                case CardEffectKind.BulletSpeedAdd:
                    foreach (var h in heroes) if (h != null) h.bulletSpeed += card.value;
                    break;
                case CardEffectKind.SpawnExtraHero:
                    ExtraHeroSpawner?.Invoke();
                    break;
                case CardEffectKind.FullHeal:
                    foreach (var h in heroes)
                    {
                        if (h == null) continue;
                        var hp = h.GetComponent<Health>();
                        hp?.Heal(hp.MaxHp);
                    }
                    break;
            }
        }
    }
}
