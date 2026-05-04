using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Data
{
    /// <summary>
    /// Реестр всех доступных рецептов. Загружает Resources/Recipes/* один раз.
    /// </summary>
    public static class RecipeBook
    {
        private static RecipeSO[] all;
        private static readonly Dictionary<string, RecipeSO> byId = new();

        public static RecipeSO[] All
        {
            get
            {
                EnsureLoaded();
                return all;
            }
        }

        public static RecipeSO Default
        {
            get
            {
                EnsureLoaded();
                if (byId.TryGetValue("healing_potion", out var hp) && hp != null) return hp;
                return all.Length > 0 ? all[0] : null;
            }
        }

        public static RecipeSO Get(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out var r) ? r : null;
        }

        public static RecipeSO Random()
        {
            EnsureLoaded();
            if (all == null || all.Length == 0) return null;
            return all[UnityEngine.Random.Range(0, all.Length)];
        }

        private static void EnsureLoaded()
        {
            if (all != null) return;
            all = Resources.LoadAll<RecipeSO>("Recipes");
            byId.Clear();
            if (all != null)
            {
                foreach (var so in all)
                    if (so != null && !string.IsNullOrEmpty(so.id))
                        byId[so.id] = so;
            }
            else
            {
                all = System.Array.Empty<RecipeSO>();
            }
        }
    }
}
