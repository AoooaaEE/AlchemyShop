using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Минималистичный туториал на первом запуске: 3 коротких подсказки подряд.
    /// Не блокирует геймплей, появляется и исчезает через несколько секунд каждая.
    /// </summary>
    public class TutorialOverlayUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _label;
        private Coroutine _routine;

        private static readonly (string text, float duration)[] Steps =
        {
            ("Двигайся стрелками ↑↓ или W/S", 5f),
            ("Враги идут справа — твои герои отбиваются автоматически", 5f),
            ("После каждой волны выбирай карту-апгрейд!", 6f),
            ("Каждая 10-я волна — БОСС акта. Не умри!", 5f),
        };

        public static TutorialOverlayUI Create(Canvas canvas)
        {
            var go = new GameObject("TutorialOverlayUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<TutorialOverlayUI>();
            ui.Build();
            ui._panel.SetActive(false);
            return ui;
        }

        private void Build()
        {
            _panel = new GameObject("Panel", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 250f);
            rt.sizeDelta = new Vector2(900f, 90f);

            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.95f, 0.85f, 0.30f, 0.95f);

            _label = MakeText(_panel.transform, "Label", "", 32, Vector2.zero, new Vector2(880f, 80f), TextAlignmentOptions.Center);
            _label.color = new Color(0.05f, 0.05f, 0.1f);
            _label.fontStyle = FontStyles.Bold;
            _label.raycastTarget = false;
        }

        public void Show()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(Run());
        }

        public void Hide()
        {
            if (_routine != null) StopCoroutine(_routine);
            _panel.SetActive(false);
        }

        private IEnumerator Run()
        {
            _panel.SetActive(true);
            foreach (var step in Steps)
            {
                _label.text = step.text;
                yield return new WaitForSecondsRealtime(step.duration);
            }
            _panel.SetActive(false);
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
