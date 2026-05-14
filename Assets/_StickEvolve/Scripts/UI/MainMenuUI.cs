using System;
using StickEvolve.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Стартовый экран. Кнопки: Играть, Магазин апгрейдов, Статистика, Выход.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private GameObject _panel;
        private Action _onPlay;
        private Action _onMetaShop;
        private Action _onStats;
        private TextMeshProUGUI _gemsLabel;

        public static MainMenuUI Create(Canvas canvas, Action onPlay, Action onMetaShop, Action onStats)
        {
            var go = new GameObject("MainMenuUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<MainMenuUI>();
            ui._onPlay = onPlay;
            ui._onMetaShop = onMetaShop;
            ui._onStats = onStats;
            ui.Build();
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
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            // Заголовок
            var titleShadow = MakeText(_panel.transform, "TitleShadow", "STICK EVOLVE", 110, new Vector2(6f, 366f), new Vector2(1000f, 140f), TextAlignmentOptions.Center);
            titleShadow.color = new Color(0f, 0f, 0f, 0.5f);
            titleShadow.fontStyle = FontStyles.Bold;

            var title = MakeText(_panel.transform, "Title", "STICK EVOLVE", 110, new Vector2(0f, 370f), new Vector2(1000f, 140f), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.92f, 0.45f);
            title.fontStyle = FontStyles.Bold;

            var subtitle = MakeText(_panel.transform, "Subtitle", "Эволюция стикмена · 100 волн · 10 актов", 30, new Vector2(0f, 280f), new Vector2(900f, 40f), TextAlignmentOptions.Center);
            subtitle.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);

            _gemsLabel = MakeText(_panel.transform, "GemsLabel", "", 32, new Vector2(0f, 220f), new Vector2(900f, 40f), TextAlignmentOptions.Center);
            _gemsLabel.color = new Color(0.85f, 0.55f, 1f);

            // Кнопки
            MakeMainButton("PlayBtn",   "ИГРАТЬ",            new Vector2(0f,  60f), new Color(0.30f, 0.75f, 0.40f), () => _onPlay?.Invoke());
            MakeMainButton("MetaBtn",   "МАГАЗИН АПГРЕЙДОВ", new Vector2(0f, -60f), new Color(0.45f, 0.40f, 0.85f), () => _onMetaShop?.Invoke());
            MakeMainButton("StatsBtn",  "СТАТИСТИКА",        new Vector2(0f,-180f), new Color(0.35f, 0.60f, 0.85f), () => _onStats?.Invoke());

#if UNITY_STANDALONE || UNITY_EDITOR
            MakeMainButton("QuitBtn",   "ВЫЙТИ",             new Vector2(0f,-300f), new Color(0.5f, 0.3f, 0.3f), Application.Quit);
#endif

            // Подпись внизу
            var hint = MakeText(_panel.transform, "Hint", "WASD / стрелки — движение героев", 24, new Vector2(0f, -450f), new Vector2(800f, 36f), TextAlignmentOptions.Center);
            hint.color = new Color(1f, 1f, 1f, 0.5f);
        }

        public void Show()
        {
            RefreshGems();
            _panel.SetActive(true);
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        public void RefreshGems()
        {
            _gemsLabel.text = $"Камней эволюции: {StickEvolve.Data.MetaProgression.MetaGems}";
        }

        private void MakeMainButton(string name, string text, Vector2 pos, Color color, Action onClick)
        {
            var btnGO = new GameObject(name, typeof(RectTransform));
            btnGO.transform.SetParent(_panel.transform, false);
            var brt = (RectTransform)btnGO.transform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = pos;
            brt.sizeDelta = new Vector2(420f, 90f);

            var img = btnGO.AddComponent<Image>();
            img.color = color;
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.highlightedColor = new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f);
            cb.pressedColor = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var label = MakeText(btnGO.transform, "Label", text, 34, Vector2.zero, new Vector2(400f, 80f), TextAlignmentOptions.Center);
            label.color = Color.white;
            label.raycastTarget = false;
            label.fontStyle = FontStyles.Bold;
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
