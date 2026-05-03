using System;
using Alchemy.Data;
using Alchemy.Gameplay;
using Alchemy.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alchemy.Core
{
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Менеджеры")]
        [SerializeField] private Economy.EconomyManager economy;
        [SerializeField] private UpgradeService          upgrades;

        public Economy.EconomyManager Economy  => economy;
        public UpgradeService          Upgrades => upgrades;

        [Header("Настройки")]
        [SerializeField] private int    targetFps      = 60;
        [SerializeField] private string firstSceneName = "Shop";

        [Header("Idle (offline)")]
        [SerializeField] private float baseIdleRatePerSec = 0.5f;
        [SerializeField] private long  maxOfflineGold     = 1000;

        public long PendingOfflineReward { get; private set; }
        public bool IsReady              { get; private set; }

        public event Action OnOfflineRewardConsumed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount = 0;

            if (economy  == null) economy  = GetComponentInChildren<Economy.EconomyManager>(true);
            if (upgrades == null) upgrades = GetComponentInChildren<UpgradeService>(true);

            var save = SaveSystem.Load();
            economy?.Init(save);
            upgrades?.Init(economy, save?.upgrades);

            // Считаем потенциальную награду, но НЕ начисляем —
            // игрок заберёт через рекламу или потеряет кнопкой "Пропустить".
            PendingOfflineReward = ComputeOfflineReward(save);

            IsReady = true;
            Debug.Log($"[GameManager] Bootstrap завершён. Pending offline reward: {PendingOfflineReward}.");
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(firstSceneName) &&
                SceneManager.GetActiveScene().name != firstSceneName)
                SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
        }

        /// <summary>Игрок посмотрел рекламу — выдаём всю награду.</summary>
        public void ClaimOfflineReward()
        {
            if (PendingOfflineReward <= 0 || economy == null) return;
            economy.AddGold(PendingOfflineReward);
            Debug.Log($"[GameManager] Игрок забрал офлайн-доход: +{PendingOfflineReward}.");
            PendingOfflineReward = 0;
            OnOfflineRewardConsumed?.Invoke();
        }

        /// <summary>Игрок пропустил — награда сгорает.</summary>
        public void SkipOfflineReward()
        {
            if (PendingOfflineReward > 0)
                Debug.Log($"[GameManager] Игрок пропустил награду: -{PendingOfflineReward} сгорело.");
            PendingOfflineReward = 0;
            OnOfflineRewardConsumed?.Invoke();
        }

        private long ComputeOfflineReward(SaveData save)
        {
            if (save == null || save.lastExitUnixTime <= 0 || economy == null) return 0;

            long now     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = Math.Max(0, now - save.lastExitUnixTime);
            elapsed      = Math.Min(elapsed, 24 * 60 * 60); // safety против оверфлоу

            long reward = (long)(elapsed * baseIdleRatePerSec * economy.IdleIncomeMultiplier);
            return Math.Min(reward, maxOfflineGold);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveAll();
        }

        private void OnApplicationQuit() => SaveAll();

        private void SaveAll()
        {
            if (economy == null) return;

            var data = economy.BuildSaveData();
            if (upgrades != null) data.upgrades = upgrades.BuildSaveData();
            data.lastExitUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            SaveSystem.Save(data);
        }
    }
}