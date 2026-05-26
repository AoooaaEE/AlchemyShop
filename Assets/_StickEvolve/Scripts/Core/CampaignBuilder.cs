using System.Collections.Generic;
using StickEvolve.Combat;
using StickEvolve.Wave;
using UnityEngine;

namespace StickEvolve.Core
{
    /// <summary>
    /// Строит все 40 уровней кампании (4 биома × 10 уровней × 10 волн = 400 волн).
    /// Все 9 базовых типов врагов вводятся к концу уровня 5 биома 1.
    /// Уровни 6-10 каждого биома вводят новые механики (через отдельные системы).
    /// </summary>
    public static class CampaignBuilder
    {
        public const int BiomeCount = 4;
        public const int LevelsPerBiome = 10;
        public const int WavesPerLevel = 10;
        public const int TotalLevels = BiomeCount * LevelsPerBiome; // 40

        private static readonly string[] BiomeNames =
        {
            "Лес", "Снежные горы", "Пустыня", "Замок"
        };

        public static LevelDefinition Build(int levelNumber)
        {
            var def = new LevelDefinition
            {
                levelNumber = Mathf.Clamp(levelNumber, 1, TotalLevels),
                biomeIndex = (Mathf.Clamp(levelNumber, 1, TotalLevels) - 1) / LevelsPerBiome,
                levelInBiome = ((Mathf.Clamp(levelNumber, 1, TotalLevels) - 1) % LevelsPerBiome) + 1,
            };
            def.biomeName = BiomeNames[def.biomeIndex];
            def.levelName = $"{def.biomeName} — {def.levelInBiome}";
            def.hasMiniBoss = def.levelInBiome == 5;
            def.hasFinalBoss = def.levelInBiome == 10;
            def.waves = BuildWavesForLevel(def);
            return def;
        }

        private static List<WaveConfig> BuildWavesForLevel(LevelDefinition def)
        {
            var waves = new List<WaveConfig>();
            int globalStart = (def.levelNumber - 1) * WavesPerLevel + 1;

            for (int w = 1; w <= WavesPerLevel; w++)
            {
                int globalWave = globalStart + (w - 1);
                bool isLastWave = (w == WavesPerLevel);
                bool isMidWave = (w == 5);

                var cfg = new WaveConfig
                {
                    waveNumber = globalWave,
                    spawnInterval = Mathf.Max(0.45f, 0.95f - globalWave * 0.005f),
                    postWaveDelay = 1.0f,
                    enemyHpMultiplier = 1f + (globalWave - 1) * 0.10f,
                    enemyDamageMultiplier = 1f + (globalWave - 1) * 0.07f,
                    enemyGoldDrop = 1 + globalWave / 4,
                    enemies = new List<WaveEnemy>()
                };

                FillWaveEnemies(cfg, def, w);

                if (isMidWave && def.hasMiniBoss)
                    cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 2 });

                if (isLastWave && def.hasFinalBoss)
                    cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Boss, count = 1 });

                waves.Add(cfg);
            }
            return waves;
        }

        // Frontload: вводим все 9 типов врагов к уровню 5 биома 1.
        // Уровни 6-10 и биомы 2-4 — смешанный состав с акцентом по биому.
        private static void FillWaveEnemies(WaveConfig cfg, LevelDefinition def, int waveInLevel)
        {
            int biome = def.biomeIndex;
            int levelInBiome = def.levelInBiome;
            int w = waveInLevel;

            if (biome == 0)
            {
                switch (levelInBiome)
                {
                    case 1:
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 4 + w });
                        return;
                    case 2:
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 5 + w });
                        if (w >= 3) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 2 + w / 2 });
                        if (w >= 6) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 });
                        return;
                    case 3:
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 5 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 3 });
                        if (w >= 2) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 });
                        if (w >= 4) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage, count = 1 });
                        if (w >= 7) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Healer, count = 1 });
                        return;
                    case 4:
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 5 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 3 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage, count = 1 });
                        if (w >= 3) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Shielder, count = 1 });
                        if (w >= 5) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Sniper, count = 1 });
                        return;
                    case 5:
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 6 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 3 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage, count = 1 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Shielder, count = 1 });
                        cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Sniper, count = 1 });
                        if (w >= 2) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Splitter, count = 1 });
                        if (w >= 5) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Bomber, count = 1 });
                        return;
                }
            }

            FillVariedWave(cfg, def, w);
        }

        private static void FillVariedWave(WaveConfig cfg, LevelDefinition def, int w)
        {
            int li = def.levelInBiome;
            cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Fighter, count = 5 + w / 2 });
            cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Runner, count = 2 + w / 3 });
            cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Tank, count = 1 + (w - 1) / 4 });
            cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Mage, count = 1 + (w - 1) / 5 });
            if (w >= 4) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Healer, count = 1 });
            if (w >= 3) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Shielder, count = 1 });
            if (w >= 5) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Sniper, count = 1 });
            if (w >= 2) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Splitter, count = 1 });
            if (w >= 6) cfg.enemies.Add(new WaveEnemy { kind = EnemyKind.Bomber, count = 1 });

            if (li >= 6)
            {
                EnemyKind featured = def.biomeIndex switch
                {
                    0 => EnemyKind.Splitter,
                    1 => EnemyKind.Tank,
                    2 => EnemyKind.Runner,
                    3 => EnemyKind.Mage,
                    _ => EnemyKind.Fighter
                };
                cfg.enemies.Add(new WaveEnemy { kind = featured, count = 1 + (li - 5) });
            }
        }
    }
}
