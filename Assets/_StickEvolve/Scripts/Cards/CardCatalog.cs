using System.Collections.Generic;
using UnityEngine;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Базовый каталог карт прототипа. Создаются в памяти, не в ассетах —
    /// чтобы избежать .asset/.meta синхронизации в начале проекта.
    /// </summary>
    public static class CardCatalog
    {
        private static List<CardSO> _cards;

        public static IReadOnlyList<CardSO> All => _cards ??= Build();

        private static List<CardSO> Build()
        {
            var list = new List<CardSO>
            {
                Make("dmg_15",   "Острее меча",     "Урон +15%",        CardEffectKind.DamageMultiplier, 1.15f, CardRarity.Common,   new Color(0.8f, 0.8f, 0.8f)),
                Make("dmg_30",   "Заточенный клинок","Урон +30%",       CardEffectKind.DamageMultiplier, 1.30f, CardRarity.Rare,     new Color(0.3f, 0.7f, 1f)),
                Make("rate_15",  "Быстрые руки",    "Скорость атаки +15%",CardEffectKind.FireRateMultiplier, 1.15f, CardRarity.Common, new Color(0.8f, 0.8f, 0.8f)),
                Make("rate_30",  "Мгновенный удар", "Скорость атаки +30%",CardEffectKind.FireRateMultiplier, 1.30f, CardRarity.Rare, new Color(0.3f, 0.7f, 1f)),
                Make("range_2",  "Орлиный глаз",    "Дальность +2",     CardEffectKind.RangeAdd, 2f, CardRarity.Common,             new Color(0.8f, 0.8f, 0.8f)),
                Make("hp_25",    "Прочная броня",   "HP +25%",          CardEffectKind.MaxHpMultiplier, 1.25f, CardRarity.Common,    new Color(0.8f, 0.8f, 0.8f)),
                Make("crit_10",  "Удачный удар",    "Шанс крита +10%",  CardEffectKind.CritChanceAdd, 0.10f, CardRarity.Rare,        new Color(0.3f, 0.7f, 1f)),
                Make("crit_5_x", "Сокрушительный",  "Множитель крита +0.5",CardEffectKind.CritMultiplierAdd, 0.5f, CardRarity.Epic, new Color(0.8f, 0.4f, 1f)),
                Make("speed_4",  "Реактив",         "Скорость пули +4", CardEffectKind.BulletSpeedAdd, 4f, CardRarity.Common,        new Color(0.8f, 0.8f, 0.8f)),
                Make("heal",     "Лечение",         "Полностью лечит героев",CardEffectKind.FullHeal, 0f, CardRarity.Rare,           new Color(0.3f, 0.9f, 0.3f)),
                Make("extra_hero","Подкрепление",   "Добавить ещё одного героя",CardEffectKind.SpawnExtraHero, 0f, CardRarity.Legendary, new Color(1f, 0.85f, 0.2f)),
            };
            return list;
        }

        private static CardSO Make(string id, string name, string desc, CardEffectKind effect, float value, CardRarity rarity, Color color)
        {
            var c = ScriptableObject.CreateInstance<CardSO>();
            c.id = id;
            c.displayName = name;
            c.description = desc;
            c.effect = effect;
            c.value = value;
            c.rarity = rarity;
            c.frameColor = color;
            return c;
        }

        public static List<CardSO> RollThree(System.Random rng = null)
        {
            rng ??= new System.Random();
            var pool = new List<CardSO>(All);
            var result = new List<CardSO>(3);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int idx = rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
