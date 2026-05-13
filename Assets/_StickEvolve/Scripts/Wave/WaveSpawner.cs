using System;
using System.Collections;
using System.Collections.Generic;
using StickEvolve.Combat;
using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Wave
{
    /// <summary>
    /// Спавнит врагов по WaveConfig. После того как все враги волны побеждены —
    /// триггерит OnWaveComplete (Bootstrapper показывает экран выбора карт).
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        public float spawnX = 6f;
        public float spawnYMin = -2.5f;
        public float spawnYMax = 2.5f;

        public List<WaveConfig> waves = new();
        public int CurrentWaveIndex { get; private set; }
        public WaveConfig CurrentWave => (CurrentWaveIndex >= 0 && CurrentWaveIndex < waves.Count) ? waves[CurrentWaveIndex] : null;

        public event Action<int> OnWaveStarted;     // wave number (1-based)
        public event Action<int> OnWaveCompleted;   // wave number (1-based)
        public event Action OnAllWavesCompleted;

        public bool IsRunning { get; private set; }
        private Coroutine _spawnRoutine;

        public void Reset(List<WaveConfig> newWaves)
        {
            StopAllCoroutines();
            waves = newWaves;
            CurrentWaveIndex = 0;
            IsRunning = false;
            EnemyRegistry.Instance.Clear();
        }

        public void StartNextWave()
        {
            if (IsRunning) return;
            if (CurrentWaveIndex >= waves.Count)
            {
                OnAllWavesCompleted?.Invoke();
                return;
            }
            _spawnRoutine = StartCoroutine(RunWave(waves[CurrentWaveIndex]));
        }

        private IEnumerator RunWave(WaveConfig cfg)
        {
            IsRunning = true;
            OnWaveStarted?.Invoke(cfg.waveNumber);

            // Подготовим плоский список спавнов
            var queue = new List<EnemyKind>();
            foreach (var slot in cfg.enemies)
                for (int i = 0; i < slot.count; i++)
                    queue.Add(slot.kind);

            for (int i = 0; i < queue.Count; i++)
            {
                SpawnOne(queue[i], cfg);
                yield return new WaitForSeconds(cfg.spawnInterval);
            }

            // Ждём, пока все враги умрут
            while (EnemyRegistry.Instance.Alive.Count > 0)
                yield return null;

            int completedNum = cfg.waveNumber;
            CurrentWaveIndex++;
            IsRunning = false;
            OnWaveCompleted?.Invoke(completedNum);

            if (CurrentWaveIndex >= waves.Count)
                OnAllWavesCompleted?.Invoke();
        }

        private void SpawnOne(EnemyKind kind, WaveConfig cfg)
        {
            float y = UnityEngine.Random.Range(spawnYMin, spawnYMax);
            Vector3 pos = new Vector3(spawnX, y, 0f);
            EnemyFactory.Spawn(kind, pos, cfg);
        }

        public void ForceKillAll()
        {
            var alive = new List<Enemy>(EnemyRegistry.Instance.Alive);
            foreach (var e in alive)
                if (e != null && e.IsAlive)
                    e.Health.TakeDamage(99999f, e.transform.position);
        }
    }
}
