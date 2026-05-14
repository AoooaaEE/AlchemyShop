using System.Collections.Generic;
using UnityEngine;

namespace StickEvolve.Data
{
    /// <summary>
    /// Описание одного мета-апгрейда: id, локализованное имя, формула эффекта,
    /// базовая цена/множитель, максимальный уровень.
    /// </summary>
    public class MetaUpgradeDef
    {
        public string id;
        public string displayName;
        public string description;
        public int baseCost;
        public float costMult;
        public int maxLevel;
        public Color color;
    }

    /// <summary>
    /// Глобальный реестр мета-апгрейдов и их уровней. Уровни сохраняются в save через
    /// LoadFromSave/SaveTo. Каждый купленный уровень добавляет в "ascension" — общий
    /// уровень сложности волн (чтобы прокачка не делала игру тривиальной).
    /// </summary>
    public static class MetaProgression
    {
        // — Каталог апгрейдов (фиксированный, описание читает магазин и применение) —
        public static readonly List<MetaUpgradeDef> Catalog = new()
        {
            new MetaUpgradeDef
            {
                id = "start_hero",
                displayName = "Соратник",
                description = "Дополнительный герой в стартовом отряде (+1 за уровень)",
                baseCost = 12, costMult = 2.6f, maxLevel = 3,
                color = new Color(0.55f, 0.95f, 0.45f),
            },
            new MetaUpgradeDef
            {
                id = "start_hp",
                displayName = "Закалка",
                description = "+10% к стартовому HP всех героев за уровень",
                baseCost = 5, costMult = 1.7f, maxLevel = 10,
                color = new Color(0.35f, 0.85f, 0.5f),
            },
            new MetaUpgradeDef
            {
                id = "start_dmg",
                displayName = "Сила удара",
                description = "+10% к стартовому урону за уровень",
                baseCost = 5, costMult = 1.7f, maxLevel = 10,
                color = new Color(0.95f, 0.55f, 0.35f),
            },
            new MetaUpgradeDef
            {
                id = "start_gold",
                displayName = "Кошель",
                description = "+15 стартового золота в начале каждого ранна",
                baseCost = 3, costMult = 1.5f, maxLevel = 12,
                color = new Color(1f, 0.85f, 0.25f),
            },
            new MetaUpgradeDef
            {
                id = "start_rerolls",
                displayName = "Удача",
                description = "+1 бесплатный реролл за уровень в каждом шопе",
                baseCost = 9, costMult = 2.4f, maxLevel = 5,
                color = new Color(0.4f, 0.75f, 1f),
            },
            new MetaUpgradeDef
            {
                id = "start_crit",
                displayName = "Зоркость",
                description = "+3% к шансу крита за уровень",
                baseCost = 7, costMult = 1.8f, maxLevel = 10,
                color = new Color(0.85f, 0.3f, 0.95f),
            },
            new MetaUpgradeDef
            {
                id = "start_fire_rate",
                displayName = "Реактивность",
                description = "+8% к скорости атаки за уровень",
                baseCost = 6, costMult = 1.75f, maxLevel = 10,
                color = new Color(0.95f, 0.7f, 0.3f),
            },
            new MetaUpgradeDef
            {
                id = "ascension_resist",
                displayName = "Закал героя",
                description = "−2% к усилению врагов за каждый купленный апгрейд (макс −50%)",
                baseCost = 18, costMult = 2.0f, maxLevel = 5,
                color = new Color(0.8f, 0.8f, 0.9f),
            },
        };

        private static readonly Dictionary<string, int> _levels = new();

        public static long MetaGems { get; private set; }

        public static int GetLevel(string id) => _levels.TryGetValue(id, out var l) ? l : 0;
        public static MetaUpgradeDef GetDef(string id) => Catalog.Find(d => d.id == id);

        /// <summary>Стоимость следующего уровня для данного апгрейда (или -1 если уже максимум).</summary>
        public static int NextCost(string id)
        {
            var def = GetDef(id);
            if (def == null) return -1;
            int lv = GetLevel(id);
            if (lv >= def.maxLevel) return -1;
            return Mathf.RoundToInt(def.baseCost * Mathf.Pow(def.costMult, lv));
        }

        /// <summary>Покупка одного уровня. Возвращает true при успехе.</summary>
        public static bool TryBuy(string id)
        {
            int cost = NextCost(id);
            if (cost < 0 || MetaGems < cost) return false;
            MetaGems -= cost;
            _levels[id] = GetLevel(id) + 1;
            return true;
        }

        public static void AddGems(long amount)
        {
            if (amount <= 0) return;
            MetaGems += amount;
        }

        /// <summary>
        /// Суммарное кол-во купленных уровней по всем апгрейдам.
        /// На этом основано "ascension scaling" — чем больше игрок прокачался,
        /// тем сильнее враги (компенсация прокачки).
        /// </summary>
        public static int TotalUpgradeLevels()
        {
            int n = 0;
            foreach (var def in Catalog)
            {
                if (def.id == "ascension_resist") continue;
                n += GetLevel(def.id);
            }
            return n;
        }

        /// <summary>
        /// Глобальный множитель HP врагов от ascension. +5% за каждый купленный уровень,
        /// но снижается купленным "Закалом героя" (ascension_resist).
        /// </summary>
        public static float EnemyHpAscensionMult()
        {
            int up = TotalUpgradeLevels();
            float baseScale = up * 0.05f;
            float resist = Mathf.Min(0.5f, GetLevel("ascension_resist") * 0.10f);
            return 1f + baseScale * (1f - resist);
        }

        /// <summary>Аналогично для урона врагов: +3% за уровень с резистом.</summary>
        public static float EnemyDamageAscensionMult()
        {
            int up = TotalUpgradeLevels();
            float baseScale = up * 0.03f;
            float resist = Mathf.Min(0.5f, GetLevel("ascension_resist") * 0.10f);
            return 1f + baseScale * (1f - resist);
        }

        public static void LoadFromSave(StickSaveData save)
        {
            _levels.Clear();
            MetaGems = save.metaGems;
            if (save.metaUpgrades != null)
                foreach (var e in save.metaUpgrades)
                    if (!string.IsNullOrEmpty(e.upgradeId)) _levels[e.upgradeId] = e.level;
        }

        public static void SaveTo(StickSaveData save)
        {
            save.metaGems = MetaGems;
            save.metaUpgrades = new List<MetaUpgradeEntry>();
            foreach (var kv in _levels)
                save.metaUpgrades.Add(new MetaUpgradeEntry { upgradeId = kv.Key, level = kv.Value });
        }

        public static void ResetAll()
        {
            _levels.Clear();
            MetaGems = 0;
        }
    }
}
