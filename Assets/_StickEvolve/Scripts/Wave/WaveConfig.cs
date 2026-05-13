using System;
using System.Collections.Generic;
using StickEvolve.Combat;

namespace StickEvolve.Wave
{
    /// <summary>
    /// Описание одной волны: какие враги, сколько, с какой задержкой.
    /// В прототипе создаётся в коде (PrototypeBootstrapper), не SO.
    /// </summary>
    [Serializable]
    public class WaveConfig
    {
        public int waveNumber;
        public float spawnInterval = 0.7f;
        public float postWaveDelay = 1.5f;
        public List<WaveEnemy> enemies = new();
        public float enemyHpMultiplier = 1f;
        public float enemyDamageMultiplier = 1f;
        public int enemyGoldDrop = 1;
    }

    [Serializable]
    public class WaveEnemy
    {
        public EnemyKind kind = EnemyKind.Fighter;
        public int count = 5;
    }
}
