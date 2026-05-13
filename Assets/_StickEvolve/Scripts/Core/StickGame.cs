using System;
using StickEvolve.Cards;
using StickEvolve.Combat;
using StickEvolve.Data;
using StickEvolve.Wave;
using UnityEngine;

namespace StickEvolve.Core
{
    /// <summary>
    /// Менеджер прототипа. Хранит ссылки, координирует поток wave → cards → next wave.
    /// </summary>
    public class StickGame : MonoBehaviour
    {
        public static StickGame Instance { get; private set; }

        public StickEconomy Economy { get; private set; }
        public WaveSpawner Spawner { get; set; }
        public int CurrentWaveNumber { get; private set; } = 1;
        public int HighestWaveCompleted { get; private set; }

        public event Action<int> OnWaveNumberChanged;
        public event Action OnGameOver;
        public event Action OnGameRestart;
        public event Action OnAllWavesCompleted;

        public bool IsGameOver { get; private set; }

        private StickSaveData _save;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Economy = new StickEconomy();
            StickGameRefs.Economy = Economy;

            _save = StickSaveSystem.Load();
            HighestWaveCompleted = _save.highestWaveCompleted;
            Economy.Reset(_save.gold);
            CardProgression.LoadFromSave(_save);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void NotifyWaveStarted(int waveNum)
        {
            CurrentWaveNumber = waveNum;
            OnWaveNumberChanged?.Invoke(waveNum);
        }

        public void NotifyWaveCompleted(int waveNum)
        {
            if (waveNum > HighestWaveCompleted)
            {
                HighestWaveCompleted = waveNum;
                _save.highestWaveCompleted = HighestWaveCompleted;
            }
            PersistSave();
        }

        public void NotifyAllWavesCompleted()
        {
            OnAllWavesCompleted?.Invoke();
        }

        public void TriggerGameOver()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            PersistSave();
            OnGameOver?.Invoke();
        }

        public void Restart()
        {
            IsGameOver = false;
            CurrentWaveNumber = 1;
            Economy.Reset(0);
            EnemyRegistry.Instance.Clear();
            HeroRegistry.Instance.Clear();
            OnGameRestart?.Invoke();
        }

        public void PersistSave()
        {
            _save.gold = Economy.Gold;
            _save.highestWaveCompleted = HighestWaveCompleted;
            CardProgression.SaveTo(_save);
            _save.lastExitUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StickSaveSystem.Save(_save);
        }

        public void ResetAllProgress()
        {
            CardProgression.ResetAll();
            HighestWaveCompleted = 0;
            _save = new StickSaveData();
            Economy.Reset(0);
            PersistSave();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) PersistSave();
        }

        private void OnApplicationQuit() => PersistSave();
    }
}
