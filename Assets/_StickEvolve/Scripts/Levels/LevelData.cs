using System;
using System.Collections.Generic;
using StickEvolve.Combat;
using StickEvolve.Wave;

namespace StickEvolve.Levels
{
    /// <summary>
    /// One playable level. Contains 10 waves, a biome, and scaling info.
    /// Levels 1-10 = Plains biome, levels 11-20 = Ice biome.
    /// Every 10th wave within a level is a boss wave.
    /// </summary>
    [Serializable]
    public class LevelData
    {
        public int levelNumber;
        public BiomeType biome;
        public string displayName;
    }

    public static class LevelCatalog
    {
        public const int WavesPerLevel = 10;
        public const int LevelsPerBiome = 10;
        public const int TotalLevels = 20;

        public static BiomeType GetBiome(int levelNumber)
        {
            if (levelNumber <= LevelsPerBiome) return BiomeType.Plains;
            return BiomeType.Ice;
        }

        public static string GetLevelName(int levelNumber)
        {
            var biome = GetBiome(levelNumber);
            int localNum = levelNumber <= LevelsPerBiome
                ? levelNumber
                : levelNumber - LevelsPerBiome;

            return biome switch
            {
                BiomeType.Ice => $"Ледяной уровень {localNum}",
                _ => $"Уровень {localNum}",
            };
        }

        public static LevelData Get(int levelNumber)
        {
            return new LevelData
            {
                levelNumber = levelNumber,
                biome = GetBiome(levelNumber),
                displayName = GetLevelName(levelNumber),
            };
        }

        /// <summary>
        /// Build 10 WaveConfigs for a given level. The global wave index continues
        /// across levels so difficulty scales continuously (level 2 wave 1 = global wave 11).
        /// </summary>
        public static List<WaveConfig> BuildWavesForLevel(int levelNumber)
        {
            var biome = GetBiome(levelNumber);
            int globalOffset = (levelNumber - 1) * WavesPerLevel;
            var waves = new List<WaveConfig>();

            for (int local = 1; local <= WavesPerLevel; local++)
            {
                int global = globalOffset + local;
                bool isBossWave = (local == WavesPerLevel);
                bool isMiniBossWave = (local == 5);

                var w = new WaveConfig
                {
                    waveNumber = local,
                    spawnInterval = UnityEngine.Mathf.Max(0.40f, 0.95f - global * 0.018f),
                    postWaveDelay = 1.0f,
                    enemyHpMultiplier = 1f + (global - 1) * 0.28f,
                    enemyDamageMultiplier = 1f + (global - 1) * 0.18f,
                    enemyGoldDrop = 1 + global / 2,
                    enemies = new List<WaveEnemy>(),
                };

                if (biome == BiomeType.Plains)
                    PopulatePlainsWave(w, global, local, isBossWave, isMiniBossWave);
                else
                    PopulateIceWave(w, global, local, isBossWave, isMiniBossWave);

                waves.Add(w);
            }

            return waves;
        }

        private static void PopulatePlainsWave(WaveConfig w, int g, int local,
            bool isBoss, bool isMiniBoss)
        {
            w.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 5 + (g * 2) / 3 });
            if (g >= 2)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner,   count = 2 + g / 3 });
            if (g >= 3)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank,     count = 1 + (g - 3) / 3 });
            if (g >= 4)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage,     count = 1 + (g - 4) / 4 });
            if (g >= 5)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Healer,   count = 1 + (g - 5) / 5 });
            if (g >= 6)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Shielder, count = 1 + (g - 6) / 4 });
            if (g >= 7)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Sniper,   count = 1 + (g - 7) / 5 });
            if (g >= 8)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Splitter, count = 1 + (g - 8) / 4 });
            if (g >= 9)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Bomber,   count = 1 + (g - 9) / 4 });

            if (isMiniBoss)
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 2 });
            if (isBoss)
            {
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.Boss, count = 1 });
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.Bomber, count = 2 });
            }
        }

        private static void PopulateIceWave(WaveConfig w, int g, int local,
            bool isBoss, bool isMiniBoss)
        {
            w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceFighter, count = 5 + (g * 2) / 3 });
            if (g >= 2)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceRunner,  count = 2 + g / 3 });
            if (g >= 4)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceMage,    count = 1 + (g - 4) / 4 });
            if (g >= 3)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceTank,    count = 1 + (g - 3) / 3 });
            if (g >= 5)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Healer,     count = 1 + (g - 5) / 5 });
            if (g >= 6)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Shielder,   count = 1 + (g - 6) / 4 });
            if (g >= 7)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Sniper,     count = 1 + (g - 7) / 5 });
            if (g >= 8)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Splitter,   count = 1 + (g - 8) / 4 });
            if (g >= 9)  w.enemies.Add(new WaveEnemy { kind = EnemyKind.Bomber,     count = 1 + (g - 9) / 4 });

            if (isMiniBoss)
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceTank, count = 2 });
            if (isBoss)
            {
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceBoss, count = 1 });
                w.enemies.Add(new WaveEnemy { kind = EnemyKind.IceMage, count = 2 });
            }
        }
    }
}
