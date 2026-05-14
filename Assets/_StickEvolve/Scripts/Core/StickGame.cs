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
        public int HighestActReached { get; private set; }
        public long TotalEnemiesKilled { get; private set; }

        // Константы структуры актов.
        public const int WavesPerAct = 10;

        // Текущий акт (1… N) и позиция внутри него (1… WavesPerAct).
        public int CurrentAct => Mathf.Max(1, (CurrentWaveNumber - 1) / WavesPerAct + 1);
        public int CurrentWaveInAct => ((CurrentWaveNumber - 1) % WavesPerAct) + 1;
        public bool IsBossWave(int waveNum) => waveNum % WavesPerAct == 0;

        public event Action<int> OnWaveNumberChanged;
        public event Action<int> OnActStarted;     // первая волна нового акта
        public event Action<int> OnActCompleted;  // босс акта убит
        public event Action OnGameOver;
        public event Action OnGameRestart;
        public event Action OnAllWavesCompleted;

        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }

        private StickSaveData _save;
        private int _lastNotifiedAct = 0;

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
            HighestActReached = _save.highestActReached;
            TotalEnemiesKilled = _save.totalEnemiesKilled;
            // Стартовое золото от мета-апгрейда "start_gold": +15 за уровень.
            MetaProgression.LoadFromSave(_save);
            int startGoldBonus = MetaProgression.GetLevel("start_gold") * 15;
            Economy.Reset(_save.gold + startGoldBonus);
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

            int act = CurrentAct;
            if (act != _lastNotifiedAct && CurrentWaveInAct == 1)
            {
                _lastNotifiedAct = act;
                if (act > HighestActReached)
                {
                    HighestActReached = act;
                    _save.highestActReached = act;
                }
                OnActStarted?.Invoke(act);
            }
        }

        public void NotifyEnemyKilled()
        {
            TotalEnemiesKilled++;
            _save.totalEnemiesKilled = TotalEnemiesKilled;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void NotifyWaveCompleted(int waveNum)
        {
            if (waveNum > HighestWaveCompleted)
            {
                HighestWaveCompleted = waveNum;
                _save.highestWaveCompleted = HighestWaveCompleted;
            }
            if (IsBossWave(waveNum))
            {
                int act = (waveNum - 1) / WavesPerAct + 1;
                OnActCompleted?.Invoke(act);
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
            // При Game Over конвертируем 12% текущего золота в мета-валюту (камни эволюции).
            long earnedGems = Mathf.Max(1, Mathf.RoundToInt(Economy.Gold * 0.12f));
            MetaProgression.AddGems(earnedGems);
            LastRunGemsEarned = earnedGems;
            PersistSave();
            OnGameOver?.Invoke();
        }

        public long LastRunGemsEarned { get; private set; }

        public void Restart()
        {
            IsGameOver = false;
            CurrentWaveNumber = 1;
            _lastNotifiedAct = 0;
            // Сбрасываем золото ранна, но выдаём стартовое золото от мета-апгрейда.
            int startGoldBonus = MetaProgression.GetLevel("start_gold") * 15;
            Economy.Reset(startGoldBonus);
            EnemyRegistry.Instance.Clear();
            HeroRegistry.Instance.Clear();
            PersistSave();
            OnGameRestart?.Invoke();
        }

        public void PersistSave()
        {
            if (_save == null || Economy == null) return;
            _save.gold = Economy.Gold;
            _save.highestWaveCompleted = HighestWaveCompleted;
            _save.highestActReached = HighestActReached;
            _save.totalEnemiesKilled = TotalEnemiesKilled;
            CardProgression.SaveTo(_save);
            MetaProgression.SaveTo(_save);
            _save.lastExitUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StickSaveSystem.Save(_save);
        }

        public StickSaveData GetSaveSnapshot() => _save;

        public void MarkTutorialShown()
        {
            _save.tutorialShown = true;
            PersistSave();
        }

        public void ResetAllProgress()
        {
            CardProgression.ResetAll();
            MetaProgression.ResetAll();
            HighestWaveCompleted = 0;
            HighestActReached = 0;
            TotalEnemiesKilled = 0;
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
