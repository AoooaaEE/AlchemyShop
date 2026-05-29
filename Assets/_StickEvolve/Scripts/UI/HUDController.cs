using StickEvolve.Combat;
using StickEvolve.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Top bar: gold (left), wave# (center), hero hp (right).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _waveText;
        private Image _hpFill;
        private TextMeshProUGUI _hpText;

        private StickGame _game;

        public static HUDController Create(Canvas canvas, StickGame game)
        {
            var go = new GameObject("HUDController");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(0f, 100f);

            var hud = go.AddComponent<HUDController>();
            hud._game = game;
            hud.BuildLayout();
            hud.HookEvents();
            hud.RefreshAll();
            return hud;
        }

        private void BuildLayout()
        {
            // Полу-прозрачный фон
            var bg = new GameObject("BG");
            bg.transform.SetParent(transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = ColorPalette.HudPanel;
            bgImg.raycastTarget = false;

            _goldText = MakeText("Gold", "GOLD: 0", 36, TextAlignmentOptions.Left);
            var goldRT = _goldText.rectTransform;
            goldRT.anchorMin = new Vector2(0f, 0.5f);
            goldRT.anchorMax = new Vector2(0f, 0.5f);
            goldRT.pivot = new Vector2(0f, 0.5f);
            goldRT.anchoredPosition = new Vector2(40f, 0f);
            goldRT.sizeDelta = new Vector2(400f, 60f);
            _goldText.color = ColorPalette.HudText;

            _waveText = MakeText("Wave", "WAVE 1", 40, TextAlignmentOptions.Center);
            var waveRT = _waveText.rectTransform;
            waveRT.anchorMin = new Vector2(0.5f, 0.5f);
            waveRT.anchorMax = new Vector2(0.5f, 0.5f);
            waveRT.pivot = new Vector2(0.5f, 0.5f);
            waveRT.anchoredPosition = new Vector2(0f, 0f);
            waveRT.sizeDelta = new Vector2(400f, 60f);
            _waveText.color = ColorPalette.HudText;

            // HP bar (right)
            var hpFrame = new GameObject("HPFrame");
            hpFrame.transform.SetParent(transform, false);
            var hpFR = hpFrame.AddComponent<RectTransform>();
            hpFR.anchorMin = new Vector2(1f, 0.5f);
            hpFR.anchorMax = new Vector2(1f, 0.5f);
            hpFR.pivot = new Vector2(1f, 0.5f);
            hpFR.anchoredPosition = new Vector2(-40f, 0f);
            hpFR.sizeDelta = new Vector2(280f, 32f);
            var frameImg = hpFrame.AddComponent<Image>();
            frameImg.color = ColorPalette.HudPanel;
            frameImg.raycastTarget = false;

            var fillGO = new GameObject("HPFill");
            fillGO.transform.SetParent(hpFrame.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = new Vector2(2f, 2f);
            fillRT.offsetMax = new Vector2(-2f, -2f);
            _hpFill = fillGO.AddComponent<Image>();
            _hpFill.color = ColorPalette.HpBarHigh;
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;
            _hpFill.raycastTarget = false;

            _hpText = MakeText("HPText", "HP", 18, TextAlignmentOptions.Center);
            var hpTRT = _hpText.rectTransform;
            hpTRT.SetParent(hpFrame.transform, false);
            hpTRT.anchorMin = Vector2.zero;
            hpTRT.anchorMax = Vector2.one;
            hpTRT.offsetMin = Vector2.zero;
            hpTRT.offsetMax = Vector2.zero;
            _hpText.color = ColorPalette.HudText;
        }

        private TextMeshProUGUI MakeText(string label, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject($"Text_{label}");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void HookEvents()
        {
            if (_game == null) return;
            _game.Economy.OnGoldChanged += OnGoldChanged;
            _game.OnWaveNumberChanged += OnWaveChanged;
        }

        private void OnDestroy()
        {
            if (_game == null) return;
            _game.Economy.OnGoldChanged -= OnGoldChanged;
            _game.OnWaveNumberChanged -= OnWaveChanged;
        }

        private void OnGoldChanged(long g)
        {
            if (_goldText != null) _goldText.text = $"GOLD: {g}";
        }

        private void OnWaveChanged(int w)
        {
            if (_waveText == null) return;
            int waveInLevel = ((w - 1) % 10) + 1;
            int level = ((w - 1) / 10) + 1;
            _waveText.text = $"Ур.{level}  ·  Волна {waveInLevel}/10";
        }

        private void RefreshAll()
        {
            if (_game == null) return;
            OnGoldChanged(_game.Economy.Gold);
            OnWaveChanged(_game.CurrentWaveNumber);
        }

        private void Update()
        {
            // Аггрегируем HP всех живых героев — в прототипе у нас 1-3 героя
            float total = 0f;
            float max = 0f;
            var heroes = HeroRegistry.Instance.Alive;
            for (int i = 0; i < heroes.Count; i++)
            {
                var h = heroes[i];
                if (h == null) continue;
                var hp = h.GetComponent<Health>();
                if (hp == null) continue;
                total += hp.CurrentHp;
                max += hp.MaxHp;
            }
            if (max <= 0f)
            {
                if (_hpFill != null) _hpFill.fillAmount = 0f;
                if (_hpText != null) _hpText.text = "DEAD";
                return;
            }
            float t = Mathf.Clamp01(total / max);
            if (_hpFill != null)
            {
                _hpFill.fillAmount = t;
                _hpFill.color = Color.Lerp(ColorPalette.HpBarLow, ColorPalette.HpBarHigh, t);
            }
            if (_hpText != null) _hpText.text = $"{Mathf.RoundToInt(total)} / {Mathf.RoundToInt(max)}";
        }
    }
}
