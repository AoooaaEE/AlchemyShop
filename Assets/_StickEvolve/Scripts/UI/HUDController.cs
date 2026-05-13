using StickEvolve.Combat;
using StickEvolve.Core;
using StickEvolve.Economy;
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
            bgImg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            bgImg.raycastTarget = false;

            _goldText = MakeText("Gold", "GOLD: 0", 36, TextAlignmentOptions.Left);
            var goldRT = _goldText.rectTransform;
            goldRT.anchorMin = new Vector2(0f, 0.5f);
            goldRT.anchorMax = new Vector2(0f, 0.5f);
            goldRT.pivot = new Vector2(0f, 0.5f);
            goldRT.anchoredPosition = new Vector2(40f, 0f);
            goldRT.sizeDelta = new Vector2(400f, 60f);
            _goldText.color = new Color(1f, 0.85f, 0.2f);
            AddHudIcon("GoldCoin", new Vector2(20f, 0f), new Vector2(34f, 34f), SpriteFactory.Circle(), new Color(1f, 0.75f, 0.15f), new Vector2(0f, 0.5f));

            _waveText = MakeText("Wave", "WAVE 1", 40, TextAlignmentOptions.Center);
            var waveRT = _waveText.rectTransform;
            waveRT.anchorMin = new Vector2(0.5f, 0.5f);
            waveRT.anchorMax = new Vector2(0.5f, 0.5f);
            waveRT.pivot = new Vector2(0.5f, 0.5f);
            waveRT.anchoredPosition = new Vector2(0f, 0f);
            waveRT.sizeDelta = new Vector2(400f, 60f);
            _waveText.color = Color.white;
            AddHudIcon("WaveSparkL", new Vector2(-170f, 0f), new Vector2(32f, 32f), SpriteFactory.Spark(), new Color(0.65f, 0.85f, 1f, 0.85f), new Vector2(0.5f, 0.5f));
            AddHudIcon("WaveSparkR", new Vector2(170f, 0f), new Vector2(32f, 32f), SpriteFactory.Spark(), new Color(0.65f, 0.85f, 1f, 0.85f), new Vector2(0.5f, 0.5f));

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
            frameImg.color = new Color(0.12f, 0.13f, 0.16f, 1f);
            frameImg.raycastTarget = false;

            var fillGO = new GameObject("HPFill");
            fillGO.transform.SetParent(hpFrame.transform, false);
            var fillRT = fillGO.AddComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(1f, 1f);
            fillRT.offsetMin = new Vector2(2f, 2f);
            fillRT.offsetMax = new Vector2(-2f, -2f);
            _hpFill = fillGO.AddComponent<Image>();
            _hpFill.color = new Color(0.35f, 0.85f, 0.4f);
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
            _hpText.color = Color.white;
        }

        private void AddHudIcon(string name, Vector2 pos, Vector2 size, Sprite sprite, Color color, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
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
            if (_waveText != null) _waveText.text = $"WAVE {w}";
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
                _hpFill.color = t > 0.5f
                    ? new Color(0.35f, 0.85f, 0.4f)
                    : (t > 0.25f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.9f, 0.3f, 0.3f));
            }
            if (_hpText != null) _hpText.text = $"{Mathf.RoundToInt(total)} / {Mathf.RoundToInt(max)}";
        }
    }
}
