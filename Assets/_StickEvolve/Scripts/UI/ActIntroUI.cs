using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Большой неблокирующий оверлей "АКТ N" в начале каждого акта,
    /// и "АКТ N ПРОЙДЕН!" в конце. Появляется и исчезает за ~2 секунды,
    /// не ставит игру на паузу.
    /// </summary>
    public class ActIntroUI : MonoBehaviour
    {
        private GameObject _panel;
        private TextMeshProUGUI _bigLabel;
        private TextMeshProUGUI _subLabel;
        private Coroutine _routine;
        private CanvasGroup _cg;

        public static ActIntroUI Create(Canvas canvas)
        {
            var go = new GameObject("ActIntroUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<ActIntroUI>();
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

            _cg = _panel.AddComponent<CanvasGroup>();
            _cg.blocksRaycasts = false;
            _cg.interactable = false;

            // Полу-прозрачная подложка
            var bg = new GameObject("BG", typeof(RectTransform));
            bg.transform.SetParent(_panel.transform, false);
            var bgRT = (RectTransform)bg.transform;
            bgRT.anchorMin = new Vector2(0f, 0.4f);
            bgRT.anchorMax = new Vector2(1f, 0.6f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.7f);
            bgImg.raycastTarget = false;

            _bigLabel = MakeText(_panel.transform, "Big", "АКТ 1", 130, new Vector2(0f, 30f), new Vector2(900f, 160f), TextAlignmentOptions.Center);
            _bigLabel.color = new Color(1f, 0.92f, 0.45f);
            _bigLabel.fontStyle = FontStyles.Bold;
            _bigLabel.raycastTarget = false;

            _subLabel = MakeText(_panel.transform, "Sub", "Волны 1 — 10", 36, new Vector2(0f, -75f), new Vector2(800f, 40f), TextAlignmentOptions.Center);
            _subLabel.color = new Color(0.95f, 0.95f, 0.95f);
            _subLabel.raycastTarget = false;
        }

        public void ShowActStart(int act)
        {
            int from = (act - 1) * 10 + 1;
            int to = act * 10;
            ShowImpl($"АКТ {act}", $"Волны {from} — {to}", new Color(1f, 0.92f, 0.45f));
        }

        public void ShowActComplete(int act)
        {
            ShowImpl($"АКТ {act} ПРОЙДЕН!", "Босс повержен", new Color(0.5f, 1f, 0.5f));
        }

        public void ShowAllComplete()
        {
            ShowImpl("ПОБЕДА!", "Все 10 актов завершены", new Color(1f, 0.95f, 0.4f));
        }

        private void ShowImpl(string big, string sub, Color color)
        {
            _bigLabel.text = big;
            _bigLabel.color = color;
            _subLabel.text = sub;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRun());
        }

        private IEnumerator FadeRun()
        {
            _panel.SetActive(true);
            _cg.alpha = 0f;
            // Fade in 0.3s
            float t = 0f;
            while (t < 0.3f) { t += Time.unscaledDeltaTime; _cg.alpha = Mathf.Clamp01(t / 0.3f); yield return null; }
            _cg.alpha = 1f;
            // Hold 1.6s
            yield return new WaitForSecondsRealtime(1.6f);
            // Fade out 0.5s
            t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; _cg.alpha = 1f - Mathf.Clamp01(t / 0.5f); yield return null; }
            _cg.alpha = 0f;
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
