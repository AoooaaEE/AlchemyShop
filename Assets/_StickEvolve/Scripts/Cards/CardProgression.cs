using System.Collections.Generic;
using StickEvolve.Combat;
using StickEvolve.Data;
using UnityEngine;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Базовые статы героя, посчитанные из всех апнутых карт.
    /// </summary>
    public class HeroDefaults
    {
        public float damage = 1.2f;
        public float fireRate = 2.2f;
        public float range = 7f;
        public float bulletSpeed = 14f;
        public float maxHp = 25f;
        public float critChance = 0f;
        public float critMultiplier = 2f;
        public int extraHeroes;
        public int multiShot;        // дополнительные снаряды
        public int bulletPierce;     // сквозные пробития
        public float lifesteal;      // 0..1
        public float thorns;         // фиксированный урон шипов
        public float goldMult = 1f;  // глобальный множитель золота
        public readonly List<HeroClass> classHires = new();
    }

    /// <summary>
    /// Прогресс по картам: словарь id → сколько раз эту карту брали.
    /// Считает агрегированные статы героя из этого словаря.
    /// Считает «pity»-счётчик: сколько common-карт подряд игрок взял — нужно для гарантированного rare+.
    /// </summary>
    public static class CardProgression
    {
        private static readonly Dictionary<string, int> _levels = new();
        public static int CommonsStreak;
        public const int PityThreshold = 10;

        /// <summary>Кэшируется в Compute(): глобальный множитель золота с убийств.</summary>
        public static float GoldMultiplier { get; private set; } = 1f;

        public static int GetLevel(string id)
        {
            return _levels.TryGetValue(id, out var l) ? l : 0;
        }

        public static IReadOnlyDictionary<string, int> AllLevels => _levels;

        /// <summary>
        /// Игрок выбрал карту. Обновляет уровень и pity-счётчик.
        /// FullHeal не считается «прогрессией», но обнуляет/инкрементит pity по своей редкости.
        /// </summary>
        public static void RegisterPick(CardSO card)
        {
            if (card == null) return;
            if (card.rarity == CardRarity.Common) CommonsStreak++;
            else CommonsStreak = 0;

            if (card.effect == CardEffectKind.FullHeal) return;
            _levels[card.id] = GetLevel(card.id) + 1;
        }

        public static HeroDefaults Compute()
        {
            var d = new HeroDefaults();

            // Мета-апгрейды применяются ПЕРВЫМИ — они модифицируют базовые статы до карт.
            int metaHp = StickEvolve.Data.MetaProgression.GetLevel("start_hp");
            int metaDmg = StickEvolve.Data.MetaProgression.GetLevel("start_dmg");
            int metaCrit = StickEvolve.Data.MetaProgression.GetLevel("start_crit");
            int metaFire = StickEvolve.Data.MetaProgression.GetLevel("start_fire_rate");
            int metaStartHero = StickEvolve.Data.MetaProgression.GetLevel("start_hero");
            d.maxHp *= 1f + 0.10f * metaHp;
            d.damage *= 1f + 0.10f * metaDmg;
            d.critChance = Mathf.Clamp01(d.critChance + 0.03f * metaCrit);
            d.fireRate *= 1f + 0.08f * metaFire;
            d.extraHeroes += metaStartHero;

            foreach (var kv in _levels)
            {
                var card = CardCatalog.GetById(kv.Key);
                if (card == null) continue;
                int times = kv.Value;
                for (int i = 0; i < times; i++) ApplyOne(d, card);
            }
            GoldMultiplier = d.goldMult;
            return d;
        }

        private static void ApplyOne(HeroDefaults d, CardSO card)
        {
            switch (card.effect)
            {
                case CardEffectKind.DamageMultiplier:    d.damage *= card.value; break;
                case CardEffectKind.FireRateMultiplier:  d.fireRate *= card.value; break;
                case CardEffectKind.RangeAdd:            d.range += card.value; break;
                case CardEffectKind.MaxHpMultiplier:     d.maxHp *= card.value; break;
                case CardEffectKind.CritChanceAdd:       d.critChance = Mathf.Clamp01(d.critChance + card.value); break;
                case CardEffectKind.CritMultiplierAdd:   d.critMultiplier += card.value; break;
                case CardEffectKind.BulletSpeedAdd:      d.bulletSpeed += card.value; break;
                case CardEffectKind.SpawnExtraHero:      d.extraHeroes += Mathf.Max(1, Mathf.RoundToInt(card.value)); break;
                case CardEffectKind.MultiShotAdd:        d.multiShot += Mathf.Max(1, Mathf.RoundToInt(card.value)); break;
                case CardEffectKind.BulletPierceAdd:     d.bulletPierce += Mathf.Max(1, Mathf.RoundToInt(card.value)); break;
                case CardEffectKind.LifestealAdd:        d.lifesteal = Mathf.Clamp01(d.lifesteal + card.value); break;
                case CardEffectKind.ThornsAdd:           d.thorns += card.value; break;
                case CardEffectKind.GoldGainMult:        d.goldMult *= card.value; break;
                case CardEffectKind.SpawnHeroOfClass:
                {
                    int n = Mathf.Max(1, Mathf.RoundToInt(card.value));
                    int classInt = Mathf.RoundToInt(card.secondaryValue);
                    var cls = (HeroClass)Mathf.Clamp(classInt, 0, System.Enum.GetValues(typeof(HeroClass)).Length - 1);
                    for (int i = 0; i < n; i++) d.classHires.Add(cls);
                    break;
                }
            }
        }

        public static void LoadFromSave(StickSaveData save)
        {
            _levels.Clear();
            CommonsStreak = 0;
            if (save == null) return;

            if (save.cardLevels != null)
            {
                foreach (var entry in save.cardLevels)
                {
                    if (string.IsNullOrEmpty(entry.cardId)) continue;
                    _levels[entry.cardId] = Mathf.Max(0, entry.level);
                }
            }
            CommonsStreak = Mathf.Max(0, save.commonsStreak);
        }

        public static void SaveTo(StickSaveData save)
        {
            if (save == null) return;
            save.cardLevels = new List<CardLevelEntry>();
            foreach (var kv in _levels)
            {
                save.cardLevels.Add(new CardLevelEntry { cardId = kv.Key, level = kv.Value });
            }
            save.commonsStreak = CommonsStreak;
        }

        public static void ResetAll()
        {
            _levels.Clear();
            CommonsStreak = 0;
        }
    }
}
