using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Полноэкранное модальное окно с 3 кнопками-картами между волнами.
    /// Создаётся целиком в коде — никаких префабов.
    /// </summary>
    public class CardChoiceUI : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private event Action<CardSO> _onPick;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        public static CardChoiceUI Create(Canvas hudCanvas)
        {
            var go = new GameObject("CardChoiceUI");
            go.transform.SetParent(hudCanvas.transform, false);
            var ui = go.AddComponent<CardChoiceUI>();
            ui._canvas = hudCanvas;
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
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            _panel.SetActive(false);
        }

        public void Show(List<CardSO> options, Action<CardSO> onPick)
        {
            _onPick = onPick;

            // Очистить старые кнопки
            for (int i = _panel.transform.childCount - 1; i >= 0; i--)
                Destroy(_panel.transform.GetChild(i).gameObject);

            // Заголовок
            var title = MakeText("Title", "ВЫБЕРИ КАРТУ", 56, new Vector2(0f, 280f), new Vector2(900f, 80f), TextAlignmentOptions.Center);
            title.color = Color.white;

            float startX = -340f;
            float dx = 340f;
            for (int i = 0; i < options.Count; i++)
            {
                BuildCard(options[i], new Vector2(startX + dx * i, 0f));
            }

            _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void BuildCard(CardSO card, Vector2 anchoredPos)
        {
            var go = new GameObject($"Card_{card.id}");
            go.transform.SetParent(_panel.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(280f, 380f);

            var img = go.AddComponent<Image>();
            img.color = card.frameColor;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(go.transform, false);
            var irt = inner.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.offsetMin = new Vector2(8f, 8f);
            irt.offsetMax = new Vector2(-8f, -8f);
            var iimg = inner.AddComponent<Image>();
            iimg.color = new Color(0.12f, 0.12f, 0.15f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var card1 = card;
            btn.onClick.AddListener(() => OnPicked(card1));

            // Текст имени
            var name = MakeText("Name", card.displayName, 28, new Vector2(0f, 130f), new Vector2(260f, 60f), TextAlignmentOptions.Center);
            name.transform.SetParent(go.transform, false);
            name.color = Color.white;

            // Текст редкости
            var rarity = MakeText("Rarity", card.rarity.ToString().ToUpper(), 18, new Vector2(0f, 80f), new Vector2(260f, 30f), TextAlignmentOptions.Center);
            rarity.transform.SetParent(go.transform, false);
            rarity.color = card.frameColor;

            // Текст описания
            var desc = MakeText("Desc", card.description, 22, new Vector2(0f, -80f), new Vector2(240f, 120f), TextAlignmentOptions.Center);
            desc.transform.SetParent(go.transform, false);
            desc.color = new Color(0.9f, 0.9f, 0.9f);
        }

        private TextMeshProUGUI MakeText(string label, string text, float fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject($"Text_{label}");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void OnPicked(CardSO card)
        {
            Hide();
            _onPick?.Invoke(card);
            _onPick = null;
        }
    }
}
