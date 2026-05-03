using UnityEngine;

namespace Alchemy.Data
{
        public enum UpgradeType { PotionPrice, Expansion, HireApprentice }

    /// <summary>
    /// Карточка апгрейда: id, тип эффекта, цена и макс. уровень.
    /// Стоимость уровня L: baseCost * (costGrowth ^ L).
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_New", menuName = "Alchemy/Upgrade", order = 1)]
    public class UpgradeSO : ScriptableObject
    {
        [Header("Идентификация")]
        public string id          = "potion_price";
        public string displayName = "Цена зелий";

        [Header("Эффект")]
        public UpgradeType type                   = UpgradeType.PotionPrice;
        public float       valuePerLevel          = 0.1f;
        public float       secondaryValuePerLevel = 0f;

        [Header("Прогрессия")]
        [Min(1)]              public int   maxLevel   = 10;
        [Min(1)]              public long  baseCost   = 10;
        [Range(1.05f, 3f)]    public float costGrowth = 1.5f;
    }
}