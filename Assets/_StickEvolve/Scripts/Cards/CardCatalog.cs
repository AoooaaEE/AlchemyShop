using System.Collections.Generic;
using StickEvolve.Combat;
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
            var grey   = new Color(0.8f, 0.8f, 0.8f);
            var blue   = new Color(0.3f, 0.7f, 1f);
            var purple = new Color(0.8f, 0.4f, 1f);
            var gold   = new Color(1f, 0.85f, 0.2f);
            var green  = new Color(0.3f, 0.9f, 0.3f);

            var list = new List<CardSO>
            {
                // — Common —
                Make("dmg_10",    "Сила руки",       "Урон +10%",                CardEffectKind.DamageMultiplier,    1.10f, 0f, CardRarity.Common,   grey),
                Make("dmg_15",    "Острее клинка",     "Урон +15%",                CardEffectKind.DamageMultiplier,    1.15f, 0f, CardRarity.Common,   grey),
                Make("rate_10",   "Ловкие пальцы",    "Скорость атаки +10%",     CardEffectKind.FireRateMultiplier,  1.10f, 0f, CardRarity.Common,   grey),
                Make("rate_15",   "Быстрые руки",     "Скорость атаки +15%",     CardEffectKind.FireRateMultiplier,  1.15f, 0f, CardRarity.Common,   grey),
                Make("range_2",   "Орлиный глаз",     "Дальность +2",             CardEffectKind.RangeAdd,            2f,    0f, CardRarity.Common,   grey),
                Make("hp_15",     "Легкая броня",     "HP +15%",                   CardEffectKind.MaxHpMultiplier,     1.15f, 0f, CardRarity.Common,   grey),
                Make("hp_25",     "Прочная броня",     "HP +25%",                   CardEffectKind.MaxHpMultiplier,     1.25f, 0f, CardRarity.Common,   grey),
                Make("speed_4",   "Реактив",          "Скорость пули +4",          CardEffectKind.BulletSpeedAdd,      4f,    0f, CardRarity.Common,   grey),

                // — Rare —
                Make("dmg_30",    "Заточенный клинок","Урон +30%",                CardEffectKind.DamageMultiplier,    1.30f, 0f, CardRarity.Rare,     blue),
                Make("rate_30",   "Мгновенный удар",  "Скорость атаки +30%",     CardEffectKind.FireRateMultiplier,  1.30f, 0f, CardRarity.Rare,     blue),
                Make("range_4",   "Снайпер",          "Дальность +4",             CardEffectKind.RangeAdd,            4f,    0f, CardRarity.Rare,     blue),
                Make("hp_50",     "Тяжёлая броня",    "HP +50%",                   CardEffectKind.MaxHpMultiplier,     1.50f, 0f, CardRarity.Rare,     blue),
                Make("crit_10",   "Удачный удар",     "Шанс крита +10%",         CardEffectKind.CritChanceAdd,       0.10f, 0f, CardRarity.Rare,     blue),
                Make("speed_7",   "Баллистика",       "Скорость пули +7",          CardEffectKind.BulletSpeedAdd,      7f,    0f, CardRarity.Rare,     blue),
                Make("heal",      "Лечение",          "Полностью лечит героев",      CardEffectKind.FullHeal,            0f,    0f, CardRarity.Rare,     green),

                // — Epic —
                Make("dmg_50",    "Истинная сила",     "Урон +50%",                CardEffectKind.DamageMultiplier,    1.50f, 0f, CardRarity.Epic,     purple),
                Make("rate_50",   "Сверхскорость",     "Скорость атаки +50%",     CardEffectKind.FireRateMultiplier,  1.50f, 0f, CardRarity.Epic,     purple),
                Make("crit_20",   "Острое чувство",    "Шанс крита +20%",         CardEffectKind.CritChanceAdd,       0.20f, 0f, CardRarity.Epic,     purple),
                Make("crit_5_x",  "Сокрушительный",   "Множитель крита +0.5",      CardEffectKind.CritMultiplierAdd,   0.5f,  0f, CardRarity.Epic,     purple),
                Make("hp_100",    "Титаническое тело","HP +100%",                 CardEffectKind.MaxHpMultiplier,     2.00f, 0f, CardRarity.Epic,     purple),

                // — Legendary —
                Make("dmg_x2",    "Ярость",            "Урон ×2",                  CardEffectKind.DamageMultiplier,    2.00f, 0f, CardRarity.Legendary, gold),
                Make("rate_x2",   "Лихорадка",         "Скорость атаки ×2",        CardEffectKind.FireRateMultiplier,  2.00f, 0f, CardRarity.Legendary, gold),
                Make("extra_hero","Подкрепление",     "+1 герой",                  CardEffectKind.SpawnExtraHero,      1f,    0f, CardRarity.Legendary, gold),
                Make("extra_hero_2","Отряд",            "+2 героя",                  CardEffectKind.SpawnExtraHero,      2f,    0f, CardRarity.Legendary, gold),

                // — Наёмники конкретных классов (Epic/Legendary, низкий шанс) —
                MakeHireCard("hire_archer",   "Найм: Лучник",    "+1 Лучник в отряд",     HeroClass.Archer,    CardRarity.Epic,      new Color(0.55f, 0.95f, 0.4f)),
                MakeHireCard("hire_mage",     "Найм: Маг",        "+1 Маг в отряд",         HeroClass.Mage,      CardRarity.Epic,      new Color(0.75f, 0.5f, 1f)),
                MakeHireCard("hire_tank",     "Найм: Танк",       "+1 Танк в отряд",        HeroClass.Tank,      CardRarity.Epic,      new Color(1f, 0.55f, 0.3f)),
                MakeHireCard("hire_healer",   "Найм: Жрец",      "+1 Жрец-лекарь",        HeroClass.Healer,    CardRarity.Legendary, new Color(0.4f, 1f, 0.6f)),
                MakeHireCard("hire_berserker","Найм: Берсерк",   "+1 Берсерк",             HeroClass.Berserker, CardRarity.Legendary, new Color(1f, 0.3f, 0.3f)),
                MakeHireCard("hire_sniper",   "Найм: Снайпер",   "+1 Снайпер",             HeroClass.Sniper,    CardRarity.Legendary, new Color(0.5f, 0.6f, 0.9f)),
                MakeHireCard("hire_ninja",    "Найм: Ниндзя",    "+1 Ниндзя",              HeroClass.Ninja,     CardRarity.Legendary, new Color(0.3f, 0.3f, 0.4f)),
            };
            return list;
        }

        private static CardSO MakeHireCard(string id, string name, string desc, HeroClass cls, CardRarity rarity, Color color)
        {
            return Make(id, name, desc, CardEffectKind.SpawnHeroOfClass, 1f, (float)(int)cls, rarity, color);
        }

        public static CardSO GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var all = All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].id == id) return all[i];
            return null;
        }

        private static CardSO Make(string id, string name, string desc, CardEffectKind effect, float value, float secondary, CardRarity rarity, Color color)
        {
            var c = ScriptableObject.CreateInstance<CardSO>();
            c.id = id;
            c.displayName = name;
            c.description = desc;
            c.effect = effect;
            c.value = value;
            c.secondaryValue = secondary;
            c.rarity = rarity;
            c.frameColor = color;
            return c;
        }

        /// <summary>
        /// 3 разных карты с весами по редкости.
        /// Если игрок взял 10 common подряд (pity в CardProgression.CommonsStreak) —
        /// хотя бы одна из 3 гарантированно будет rare+.
        /// </summary>
        public static List<CardSO> RollThree(System.Random rng = null)
        {
            rng ??= new System.Random();
            var pool = new List<CardSO>(All);
            var result = new List<CardSO>(3);

            bool pityActive = CardProgression.CommonsStreak >= CardProgression.PityThreshold;
            bool rolledRarePlus = false;

            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                bool isLastSlot = i == 2;
                List<CardSO> eligible = pool;
                if (pityActive && !rolledRarePlus && isLastSlot)
                {
                    eligible = pool.FindAll(c => c.rarity != CardRarity.Common);
                    if (eligible.Count == 0) eligible = pool;
                }

                var pick = WeightedPick(eligible, rng);
                if (pick == null) break;
                result.Add(pick);
                pool.Remove(pick);
                if (pick.rarity != CardRarity.Common) rolledRarePlus = true;
            }
            return result;
        }

        private static CardSO WeightedPick(List<CardSO> pool, System.Random rng)
        {
            if (pool.Count == 0) return null;
            float total = 0f;
            for (int i = 0; i < pool.Count; i++) total += RarityWeight(pool[i].rarity);
            if (total <= 0f) return pool[rng.Next(pool.Count)];
            float roll = (float)(rng.NextDouble() * total);
            float acc = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += RarityWeight(pool[i].rarity);
                if (roll <= acc) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private static float RarityWeight(CardRarity r) => r switch
        {
            CardRarity.Common    => 60f,
            CardRarity.Rare      => 25f,
            CardRarity.Epic      => 12f,
            CardRarity.Legendary => 3f,
            _ => 1f,
        };
    }
}
