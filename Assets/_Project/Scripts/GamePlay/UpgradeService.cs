using System;
using System.Collections.Generic;
using Alchemy.Data;
using Alchemy.Economy;
using UnityEngine;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Хранит уровни апгрейдов, применяет эффекты к экономике, поддерживает покупку.
    /// </summary>
    public class UpgradeService : MonoBehaviour
    {
        public static UpgradeService Instance { get; private set; }

        private readonly Dictionary<string, int>        levels   = new();
        private readonly Dictionary<string, UpgradeSO>  catalog  = new();

        private EconomyManager economy;

        public event Action<string, int> OnUpgradeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Init(EconomyManager econ, List<SavedUpgrade> savedLevels)
        {
            economy = econ;

            catalog.Clear();
            foreach (var so in Resources.LoadAll<UpgradeSO>("Upgrades"))
                catalog[so.id] = so;

            levels.Clear();
            if (savedLevels != null)
                foreach (var s in savedLevels)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        levels[s.id] = s.level;

            // Применяем эффекты согласно текущим уровням.
            foreach (var kv in catalog) Apply(kv.Value);
        }

        public int GetLevel(string id) => levels.TryGetValue(id, out var lvl) ? lvl : 0;

        public UpgradeSO GetUpgrade(string id) => catalog.TryGetValue(id, out var so) ? so : null;

        public bool IsMaxed(string id)
        {
            var so = GetUpgrade(id);
            return so == null || GetLevel(id) >= so.maxLevel;
        }

        public long GetCost(string id)
        {
            var so = GetUpgrade(id);
            if (so == null) return -1;
            int lvl = GetLevel(id);
            if (lvl >= so.maxLevel) return -1;
            return (long)Mathf.Ceil(so.baseCost * Mathf.Pow(so.costGrowth, lvl));
        }

        public bool TryBuy(string id)
        {
            if (economy == null) return false;
            var so = GetUpgrade(id);
            if (so == null) return false;

            int lvl = GetLevel(id);
            if (lvl >= so.maxLevel) return false;

            long cost = GetCost(id);
            if (!economy.TrySpendGold(cost)) return false;

            levels[id] = lvl + 1;
            Apply(so);
            OnUpgradeChanged?.Invoke(id, levels[id]);
            return true;
        }
        
        /// <summary>
        /// Поднять уровень без проверки стоимости (золото уже списано вручную, например, на паде).
        /// </summary>
        public bool ForceUpgrade(string id)
        {
            var so = GetUpgrade(id);
            if (so == null) return false;
            int lvl = GetLevel(id);
            if (lvl >= so.maxLevel) return false;

            levels[id] = lvl + 1;
            Apply(so);
            OnUpgradeChanged?.Invoke(id, levels[id]);
            return true;
        }

        public List<SavedUpgrade> BuildSaveData()
        {
            var result = new List<SavedUpgrade>(levels.Count);
            foreach (var kv in levels)
                result.Add(new SavedUpgrade { id = kv.Key, level = kv.Value });
            return result;
        }

                private void Apply(UpgradeSO so)
        {
            int lvl = GetLevel(so.id);
            switch (so.type)
            {
                case UpgradeType.PotionPrice:
                    economy.SetPotionPriceMultiplier(1f + lvl * so.valuePerLevel);
                    break;

                case UpgradeType.Expansion:
                    if (CustomerQueue.Instance != null)
                    {
                        int extraSlots = Mathf.RoundToInt(lvl * so.valuePerLevel);
                        CustomerQueue.Instance.SetMaxSize(4 + extraSlots);
                    }
                    if (CustomerSpawner.Instance != null)
                    {
                        float reduced = 3.5f - lvl * so.secondaryValuePerLevel;
                        CustomerSpawner.Instance.SetInterval(reduced);
                    }
                    break;
                                    case UpgradeType.HireApprentice:
                    // Сам спавн делает ShopSceneBootstrapper, подписываясь на OnUpgradeChanged.
                    break;
            }
        }
    }
}