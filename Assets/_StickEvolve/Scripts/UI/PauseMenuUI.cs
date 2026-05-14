using System;
using StickEvolve.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Меню паузы. Показывается по клику на кнопку паузы в HUD.
    /// Ставит Time.timeScale=0 пока открыто.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private GameObject _panel;
        private Action _onResume;
        private Action _onRestart;
        private Action _onMainMenu;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static PauseMenuUI Create(Canvas canvas, Action onResume, Action onRestart, Action onMainMenu)
        {
            var go = new GameObject("PauseMenuUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<PauseMenuUI>();
            ui._onResume = onResume;
            ui._onRestart = onRestart;
            ui._onMainMenu = onMainMenu;
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
            bg.color = new Color(0f, 0f, 0f, 0.78f);

            var title = MakeText(_panel.transform, "Title", "ПАУЗА", 96, new Vector2(0f, 200f), new Vector2(700f, 130f), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.95f, 0.55f);
            title.fontStyle = FontStyles.Bold;

            MakeBtn("ResumeBtn",  "ПРОДОЛЖИТЬ",  new Vector2(0f, 30f),   new Color(0.30f, 0.75f, 0.40f), () => _onResume?.Invoke());
            MakeBtn("RestartBtn", "ПЕРЕЗАПУСК",  new Vector2(0f, -90f),  new Color(0.85f, 0.55f, 0.30f), () => _onRestart?.Invoke());
            MakeBtn("MenuBtn",    "В МЕНЮ",      new Vector2(0f, -210f), new Color(0.45f, 0.45f, 0.6f),  () => _onMainMenu?.Invoke());
        }

        private void MakeBtn(string name, string text, Vector2 pos, Color color, Action onClick)
        {
            var btnGO = new GameObject(name, typeof(RectTransform));
            btnGO.transform.SetParent(_panel.transform, false);
            var brt = (RectTransform)btnGO.transform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(380f, 90f);

            var img = btnGO.AddComponent<Image>();
            img.color = color;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var label = MakeText(btnGO.transform, "Label", text, 30, Vector2.zero, new Vector2(360f, 80f), TextAlignmentOptions.Center);
            label.color = Color.white;
            label.raycastTarget = false;
            label.fontStyle = FontStyles.Bold;
        }

        public void Show()
        {
            _panel.SetActive(true);
            StickGame.Instance?.SetPaused(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
            StickGame.Instance?.SetPaused(false);
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
