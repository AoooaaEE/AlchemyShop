using System.Collections.Generic;
using StickEvolve.Data;
using UnityEngine;

namespace StickEvolve.Core
{
    /// <summary>
    /// Прогресс игрока по 40 уровням кампании.
    /// Хранит сколько звёзд (0-3) на каждом уровне и какой максимальный разблокирован.
    /// Сохраняется/загружается через StickSaveData (по образцу CardProgression).
    /// </summary>
    public static class LevelProgress
    {
        // Ключ: номер уровня 1..40. Значение: количество звёзд 1-3.
        private static readonly Dictionary<int, int> _stars = new();
        public static int HighestUnlockedLevel { get; private set; } = 1;

        public static bool IsUnlocked(int level) => level <= HighestUnlockedLevel;
        public static bool IsCompleted(int level) => GetStars(level) > 0;

        public static int GetStars(int level)
        {
            return _stars.TryGetValue(level, out var s) ? s : 0;
        }

        public static void MarkCompleted(int level, int stars)
        {
            stars = Mathf.Clamp(stars, 1, 3);
            int prev = GetStars(level);
            if (stars > prev) _stars[level] = stars;
            else if (prev == 0) _stars[level] = stars;

            int nextUnlock = level + 1;
            if (nextUnlock > HighestUnlockedLevel && nextUnlock <= CampaignBuilder.TotalLevels)
                HighestUnlockedLevel = nextUnlock;
        }

        public static int TotalStars()
        {
            int sum = 0;
            foreach (var kv in _stars) sum += kv.Value;
            return sum;
        }

        public static void LoadFromSave(StickSaveData save)
        {
            _stars.Clear();
            HighestUnlockedLevel = 1;
            if (save == null) return;
            HighestUnlockedLevel = Mathf.Clamp(save.highestUnlockedLevel, 1, CampaignBuilder.TotalLevels);
            if (save.levelStars != null)
            {
                for (int i = 0; i < save.levelStars.Count && i < CampaignBuilder.TotalLevels; i++)
                {
                    int level = i + 1;
                    int stars = Mathf.Clamp(save.levelStars[i], 0, 3);
                    if (stars > 0) _stars[level] = stars;
                }
            }
        }

        public static void SaveTo(StickSaveData save)
        {
            if (save == null) return;
            save.highestUnlockedLevel = Mathf.Max(1, HighestUnlockedLevel);
            save.levelStars = new List<int>(CampaignBuilder.TotalLevels);
            for (int i = 0; i < CampaignBuilder.TotalLevels; i++)
            {
                int level = i + 1;
                save.levelStars.Add(_stars.TryGetValue(level, out var s) ? s : 0);
            }
        }

        public static void ResetAll()
        {
            _stars.Clear();
            HighestUnlockedLevel = 1;
        }
    }
}
