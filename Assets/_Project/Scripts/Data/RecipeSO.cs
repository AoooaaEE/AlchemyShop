using UnityEngine;

namespace Alchemy.Data
{
    /// <summary>
    /// Карточка рецепта зелья. Создаётся через Create → Alchemy → Recipe.
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "Alchemy/Recipe", order = 0)]
    public class RecipeSO : ScriptableObject
    {
        [Header("Идентификация")]
        public string id = "healing_potion";
        public string displayName = "Зелье лечения";

        [Header("Тайминги (секунды)")]
        [Min(0.1f)] public float pickupTime = 0.6f;
        [Min(0.1f)] public float brewTime   = 3f;
        [Min(0.1f)] public float bottleTime = 0.6f;

        [Header("Экономика")]
        [Min(1)] public long basePrice = 10;

        [Header("Визуал")]
        public Color iconColor = new Color(1f, 0.35f, 0.35f);
    }
}