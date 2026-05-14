using System;
using StickEvolve.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Полноэкранное окно «GAME OVER» с кнопкой «Restart».
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _stats;
        private TextMeshProUGUI _gemsLabel;
        private Action _onRestart;
        private Action _onMainMenu;

        public static GameOverUI Create(Canvas canvas, Action onRestart, Action onMainMenu)
        {
            var go = new GameObject("GameOverUI");
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<GameOverUI>();
            ui._onRestart = onRestart;
            ui._onMainMenu = onMainMenu;
            ui.BuildHidden();
            return ui;
        }

        private void BuildHidden()
        {
            _panel = new GameObject("Panel");
            _panel.transform.SetParent(transform, false);
            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);

            var title = MakeText(_panel.transform, "Title", "GAME OVER", 72, TextAlignmentOptions.Center, new Vector2(0f, 160f), new Vector2(800f, 100f));
            title.color = new Color(0.95f, 0.3f, 0.3f);

            AddSpark(_panel.transform, "SparkL", new Vector2(-250f, 162f), 70f, new Color(1f, 0.55f, 0.20f, 0.8f));
            AddSpark(_panel.transform, "SparkR", new Vector2(250f, 162f), 70f, new Color(1f, 0.55f, 0.20f, 0.8f));

            _stats = MakeText(_panel.transform, "Stats", "Достигнута волна: 0", 32, TextAlignmentOptions.Center, new Vector2(0f, 60f), new Vector2(700f, 60f));
            _stats.color = Color.white;

            _gemsLabel = MakeText(_panel.transform, "Gems", "+0 камней", 30, TextAlignmentOptions.Center, new Vector2(0f, 10f), new Vector2(700f, 50f));
            _gemsLabel.color = new Color(0.85f, 0.55f, 1f);

            // Кнопка restart
            MakeButton("RestartBtn", "ПОВТОРИТЬ", new Vector2(0f, -90f),  new Color(0.3f, 0.7f, 0.4f), () => OnRestartClicked());
            MakeButton("MenuBtn",    "В МЕНЮ",     new Vector2(0f, -200f), new Color(0.45f, 0.45f, 0.6f), () => OnMainMenuClicked());

            _panel.SetActive(false);
        }

        private void MakeButton(string name, string text, Vector2 pos, Color color, Action onClick)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(_panel.transform, false);
            var brt = btnGO.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(360f, 90f);
            var img = btnGO.AddComponent<Image>();
            img.color = color;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            var btnText = MakeText(btnGO.transform, "Label", text, 32, TextAlignmentOptions.Center, Vector2.zero, new Vector2(340f, 80f));
            btnText.color = Color.white;
            btnText.raycastTarget = false;
        }

        private void AddSpark(Transform parent, string name, Vector2 pos, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.AddComponent<Image>();
            img.sprite = SpriteFactory.Spark();
            img.color = color;
            img.raycastTarget = false;
        }

        public void Show(int reachedWave, long gemsEarned = 0)
        {
            if (_stats != null) _stats.text = $"Достигнута волна: {reachedWave}";
            if (_gemsLabel != null) _gemsLabel.text = gemsEarned > 0 ? $"+ {gemsEarned} камней эволюции" : "";
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnRestartClicked()
        {
            Hide();
            _onRestart?.Invoke();
        }

        private void OnMainMenuClicked()
        {
            Hide();
            _onMainMenu?.Invoke();
        }

        private TextMeshProUGUI MakeText(Transform parent, string label, string text, float size, TextAlignmentOptions align, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject($"Text_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
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
