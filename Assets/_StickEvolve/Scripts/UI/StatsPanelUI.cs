using System;
using StickEvolve.Core;
using StickEvolve.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Простая панель статистики из главного меню: лучшая волна, лучший акт,
    /// убито врагов, накоплено камней.
    /// </summary>
    public class StatsPanelUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _stats;
        private Action _onClose;

        public static StatsPanelUI Create(Canvas canvas, Action onClose)
        {
            var go = new GameObject("StatsPanelUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<StatsPanelUI>();
            ui._onClose = onClose;
            ui.Build();
            ui._panel.SetActive(false);
            return ui;
        }

        private void Build()
        {
            _panel = new GameObject("Panel", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.10f, 0.95f);

            var title = MakeText(_panel.transform, "Title", "СТАТИСТИКА", 64, new Vector2(0f, 280f), new Vector2(900f, 80f), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.92f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            _stats = MakeText(_panel.transform, "Stats", "", 32, new Vector2(0f, 0f), new Vector2(800f, 400f), TextAlignmentOptions.Left);
            _stats.color = Color.white;
            _stats.lineSpacing = 14f;

            // Кнопка "Назад"
            var backGO = new GameObject("BackBtn", typeof(RectTransform));
            backGO.transform.SetParent(_panel.transform, false);
            var bRT = (RectTransform)backGO.transform;
            bRT.anchorMin = new Vector2(0.5f, 0f);
            bRT.anchorMax = new Vector2(0.5f, 0f);
            bRT.pivot = new Vector2(0.5f, 0f);
            bRT.anchoredPosition = new Vector2(0f, 80f);
            bRT.sizeDelta = new Vector2(360f, 90f);
            var bImg = backGO.AddComponent<Image>();
            bImg.color = new Color(0.45f, 0.5f, 0.6f);
            var bBtn = backGO.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() => _onClose?.Invoke());
            var bLabel = MakeText(backGO.transform, "Label", "НАЗАД", 32, Vector2.zero, new Vector2(340f, 80f), TextAlignmentOptions.Center);
            bLabel.color = Color.white;
            bLabel.raycastTarget = false;
            bLabel.fontStyle = FontStyles.Bold;
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
        }

        public void Hide() => _panel.SetActive(false);

        private void Refresh()
        {
            var game = StickGame.Instance;
            int wave = game != null ? game.HighestWaveCompleted : 0;
            int act = game != null ? game.HighestActReached : 0;
            long killed = game != null ? game.TotalEnemiesKilled : 0;
            long gems = MetaProgression.MetaGems;
            int upgrades = MetaProgression.TotalUpgradeLevels();

            _stats.text =
                $"<color=#FFD040>Лучшая волна:</color>     <b>{wave}</b>\n\n" +
                $"<color=#FFD040>Лучший акт:</color>        <b>{act}</b>\n\n" +
                $"<color=#FFD040>Убито врагов:</color>      <b>{killed}</b>\n\n" +
                $"<color=#D88AFF>Камней эволюции:</color>  <b>{gems}</b>\n\n" +
                $"<color=#FFA060>Куплено апгрейдов:</color> <b>{upgrades}</b>";
        }

        private TextMeshProUGUI MakeText(Transform parent, string label, string text, float size, Vector2 pos, Vector2 sizeDelta, TextAlignmentOptions align)
        {
            var go = new GameObject($"Text_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            return tmp;
        }
    }
}
