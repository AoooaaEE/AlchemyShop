using System;
using System.Collections.Generic;
using Alchemy.Core;
using Alchemy.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alchemy.UI
{
    /// <summary>
    /// HUD: золото, очередь, кнопки апгрейдов, попап «Офлайн-доход за рекламу».
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Serializable]
        public class UpgradeButtonBinding
        {
            public string             upgradeId;
            public Button             button;
            public TextMeshProUGUI    label;
        }

        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI queueText;

        [SerializeField] private List<UpgradeButtonBinding> upgradeButtons = new();

        [SerializeField] private GameObject       offlinePanel;
        [SerializeField] private TextMeshProUGUI  offlineText;
        [SerializeField] private Button           watchAdButton;
        [SerializeField] private TextMeshProUGUI  watchAdLabel;
        [SerializeField] private Button           skipButton;

        public void Setup(TextMeshProUGUI gold, TextMeshProUGUI queue,
                          List<UpgradeButtonBinding> upgrades,
                          GameObject offlinePanelGo, TextMeshProUGUI offlineLabel,
                          Button watchAdBtn, TextMeshProUGUI watchAdBtnLabel, Button skipBtn)
        {
            goldText        = gold;
            queueText       = queue;
            upgradeButtons  = upgrades ?? new List<UpgradeButtonBinding>();
            offlinePanel    = offlinePanelGo;
            offlineText     = offlineLabel;
            watchAdButton   = watchAdBtn;
            watchAdLabel    = watchAdBtnLabel;
            skipButton      = skipBtn;
        }

        private void Start()
        {
            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy != null)
            {
                economy.OnGoldChanged += OnGoldChanged;
                OnGoldChanged(economy.Gold);
            }

            foreach (var b in upgradeButtons)
            {
                if (b == null || b.button == null) continue;
                var captured = b;
                b.button.onClick.AddListener(() => OnUpgradeClick(captured.upgradeId));
            }

            if (UpgradeService.Instance != null)
                UpgradeService.Instance.OnUpgradeChanged += OnUpgradeChanged;

            if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAdClick);
            if (skipButton    != null) skipButton.onClick.AddListener(OnSkipClick);

            RefreshAllUpgrades();
            ShowOfflinePopupIfNeeded();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null && GameManager.Instance.Economy != null)
                GameManager.Instance.Economy.OnGoldChanged -= OnGoldChanged;

            if (UpgradeService.Instance != null)
                UpgradeService.Instance.OnUpgradeChanged -= OnUpgradeChanged;

            if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAdClick);
            if (skipButton    != null) skipButton.onClick.RemoveListener(OnSkipClick);
        }

        private void Update()
        {
            if (queueText != null && CustomerQueue.Instance != null)
                queueText.text = $"Очередь: {CustomerQueue.Instance.Count}";
        }

                private void OnGoldChanged(long value)
        {
            if (goldText != null) goldText.text = $"{value}";
            RefreshInteractables();
        }

        private void OnUpgradeChanged(string id, int level) => RefreshAllUpgrades();
        private void OnUpgradeClick(string id) => UpgradeService.Instance?.TryBuy(id);

        private void RefreshAllUpgrades()
        {
            if (UpgradeService.Instance == null) return;

            foreach (var b in upgradeButtons)
            {
                if (b == null || b.label == null) continue;
                var so   = UpgradeService.Instance.GetUpgrade(b.upgradeId);
                int lvl  = UpgradeService.Instance.GetLevel(b.upgradeId);
                long cost = UpgradeService.Instance.GetCost(b.upgradeId);

                if (so == null)
                    b.label.text = "—";
                else if (cost < 0)
                    b.label.text = $"{so.displayName}\nlvl {lvl} (MAX)";
                else
                    b.label.text = $"{so.displayName}\nlvl {lvl} → {cost} золота";
            }
            RefreshInteractables();
        }

        private void RefreshInteractables()
        {
            if (UpgradeService.Instance == null) return;
            long gold = GameManager.Instance != null && GameManager.Instance.Economy != null
                        ? GameManager.Instance.Economy.Gold : 0;

            foreach (var b in upgradeButtons)
            {
                if (b == null || b.button == null) continue;
                long cost = UpgradeService.Instance.GetCost(b.upgradeId);
                b.button.interactable = cost > 0 && gold >= cost;
            }
        }

        private void ShowOfflinePopupIfNeeded()
        {
            if (offlinePanel == null) return;

            long reward = GameManager.Instance != null ? GameManager.Instance.PendingOfflineReward : 0;
            if (reward <= 0)
            {
                offlinePanel.SetActive(false);
                return;
            }

            offlinePanel.SetActive(true);
            if (offlineText  != null) offlineText.text  = $"Пока тебя не было\nты мог заработать\n+{reward} золота";
            if (watchAdLabel != null) watchAdLabel.text = $"Смотреть рекламу\n(+{reward})";
        }

        private void OnWatchAdClick()
        {
            // Заглушка под рекламу. Настоящая LevelPlay подключится в Шаге 14.
            Debug.Log("[Ads] (заглушка) Реклама показана. Награда выдана.");
            GameManager.Instance?.ClaimOfflineReward();
            if (offlinePanel != null) offlinePanel.SetActive(false);
        }

        private void OnSkipClick()
        {
            GameManager.Instance?.SkipOfflineReward();
            if (offlinePanel != null) offlinePanel.SetActive(false);
        }
    }
}