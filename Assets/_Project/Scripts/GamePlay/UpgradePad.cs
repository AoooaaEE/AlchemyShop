using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alchemy.Core;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// World-space пад апгрейда. Игрок встал на него — золото плавно списывается
    /// в счётчик. Когда счётчик == цене — апгрейд применяется. Сошёл — возврат золота.
    /// </summary>
    public class UpgradePad : MonoBehaviour
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private float  interactionRadius = 1.6f;
        [SerializeField] private float  drainInterval     = 0.05f;
        // Сколько секунд игрок должен простоять, чтобы оплатить покупку — фиксированно
        // на любом уровне любого апгрейда. Раньше время = cost * drainInterval, на высоких
        // уровнях это было слишком долго.
        [SerializeField] private float  fillTime          = 4f;
        [SerializeField] private Vector3 labelOffset      = new Vector3(0f, 1.6f, 0f);

        private long  paid;
        private float drainAccum;

        private GameObject       labelRoot;
        private Image            barFill;
        private TextMeshProUGUI  titleLabel;
        private TextMeshProUGUI  costLabel;

        public void Configure(string id) { upgradeId = id; }

        private void Start()
        {
            CreateLabel();
            if (UpgradeService.Instance != null)
                UpgradeService.Instance.OnUpgradeChanged += OnUpgradeChanged;
            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (UpgradeService.Instance != null)
                UpgradeService.Instance.OnUpgradeChanged -= OnUpgradeChanged;
        }

        private void OnUpgradeChanged(string id, int level)
        {
            if (id != upgradeId) return;
            paid = 0;
            drainAccum = 0;
            RefreshLabel();
        }

        private void Update()
        {
            var svc = UpgradeService.Instance;
            if (svc == null || string.IsNullOrEmpty(upgradeId)) return;

            long cost = svc.GetCost(upgradeId);
            if (cost < 0) { gameObject.SetActive(false); return; } // максимум — пад прячется

            var player = PlayerController.Instance;
            if (player == null) { ResetProgress(); return; }

            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > interactionRadius) { ResetProgress(); return; }

            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy == null) return;

            // Сколько монет за тик, чтобы вся покупка длилась ~fillTime секунд независимо от cost.
            int  ticksTotal     = Mathf.Max(1, Mathf.RoundToInt(fillTime / Mathf.Max(0.01f, drainInterval)));
            long goldPerTickDyn = System.Math.Max(1L, (cost + ticksTotal - 1) / ticksTotal);

            drainAccum += Time.deltaTime;
            while (drainAccum >= drainInterval && paid < cost && economy.Gold > 0)
            {
                long take = System.Math.Min(goldPerTickDyn, cost - paid);
                if (!economy.TrySpendGold(take)) break;
                paid += take;
                drainAccum -= drainInterval;
            }
            if (drainAccum > drainInterval) drainAccum = drainInterval;

            if (paid >= cost)
            {
                paid = 0;
                drainAccum = 0;
                svc.ForceUpgrade(upgradeId);
            }

            RefreshLabel();
        }

        private void ResetProgress()
        {
            if (paid > 0)
            {
                var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
                if (economy != null) economy.AddGold(paid);
                paid = 0;
            }
            drainAccum = 0;
            RefreshLabel();
        }

                private void RefreshLabel()
        {
            if (UpgradeService.Instance == null) return;
            var so = UpgradeService.Instance.GetUpgrade(upgradeId);
            if (so == null) return;

            int  lvl  = UpgradeService.Instance.GetLevel(upgradeId);
            long cost = UpgradeService.Instance.GetCost(upgradeId);

            if (titleLabel != null) titleLabel.text = so.displayName;
            if (costLabel  != null) costLabel.text  = cost < 0 ? "MAX" : $"{cost}";
            if (barFill    != null) barFill.fillAmount = cost > 0 ? Mathf.Clamp01((float)paid / cost) : 0f;
        }
                private static Sprite cachedDroplet;
        public static Sprite GetDropletSprite()
        {
            if (cachedDroplet != null) return cachedDroplet;
            int   w = 96, h = 128;
            var   tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float cx      = w * 0.5f;
            float cyBot   = h * 0.42f;
            float bottomR = w * 0.45f;
            float tipY    = h * 0.95f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float halfWidth;
                if (y <= cyBot)
                {
                    float dy = (y - cyBot) / bottomR;
                    halfWidth = dy < -1f ? -1f : bottomR * Mathf.Sqrt(Mathf.Max(0f, 1f - dy * dy));
                }
                else
                {
                    float t = (y - cyBot) / (tipY - cyBot);
                    halfWidth = t > 1f ? 0f : bottomR * Mathf.Pow(1f - t, 1.4f);
                }

                float a;
                float dx = Mathf.Abs(x - cx);
                if      (halfWidth <= 0f)         a = 0f;
                else if (dx <= halfWidth - 1f)    a = 1f;
                else if (dx >= halfWidth)         a = 0f;
                else                              a = 1f - (dx - (halfWidth - 1f));

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            cachedDroplet = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            return cachedDroplet;
        }

        private static Sprite cachedRounded;
        private static Sprite GetRoundedRectSprite()
        {
            if (cachedRounded != null) return cachedRounded;
            int   size   = 128;
            float radius = 24f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float a;
                if (dist <= radius - 1f) a = 1f;
                else if (dist >= radius) a = 0f;
                else a = 1f - (dist - (radius - 1f));

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();

            cachedRounded = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            return cachedRounded;
        }

        private static Sprite cachedCircle;
        private static Sprite GetCircleSprite()
        {
            if (cachedCircle != null) return cachedCircle;
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float center = size * 0.5f;
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = dist <= radius - 1f ? 1f : (dist >= radius ? 0f : 1f - (dist - (radius - 1f)));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            cachedCircle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCircle;
        }

                        private void CreateLabel()
        {
            labelRoot = new GameObject("PadLabel");
            labelRoot.transform.SetParent(transform, false);
            labelRoot.transform.localPosition = labelOffset;

            var canvas = labelRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            labelRoot.AddComponent<CanvasScaler>();

            var rt = labelRoot.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(280f, 360f);
            rt.localScale = Vector3.one * 0.005f; // итоговый мир-размер ≈ 1.4 x 1.8

            // Внешняя белая обводка-карточка (скруглённый рамочный фон).
            var outerGo = new GameObject("Outer");
            outerGo.transform.SetParent(labelRoot.transform, false);
            var outerRt = outerGo.AddComponent<RectTransform>();
            outerRt.anchorMin = Vector2.zero;
            outerRt.anchorMax = Vector2.one;
            outerRt.offsetMin = Vector2.zero;
            outerRt.offsetMax = Vector2.zero;
            var outerImg = outerGo.AddComponent<Image>();
            outerImg.sprite = GetRoundedRectSprite();
            outerImg.type   = Image.Type.Sliced;
            outerImg.color  = new Color(1f, 1f, 1f, 0.95f);

            // Внутренняя тёмная плашка карточки.
            var innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(labelRoot.transform, false);
            var innerRt = innerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(8f, 8f);
            innerRt.offsetMax = new Vector2(-8f, -8f);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.sprite = GetRoundedRectSprite();
            innerImg.type   = Image.Type.Sliced;
            innerImg.color  = new Color(0.13f, 0.18f, 0.16f, 0.95f);

            // Title (название апгрейда).
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(innerGo.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.06f, 0.55f);
            titleRt.anchorMax = new Vector2(0.94f, 0.92f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            titleLabel.alignment        = TextAlignmentOptions.Center;
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin      = 28f;
            titleLabel.fontSizeMax      = 80f;
            titleLabel.fontStyle        = FontStyles.Bold;
            titleLabel.color            = Color.white;
            titleLabel.outlineColor     = new Color32(0, 0, 0, 200);
            titleLabel.outlineWidth     = 0.18f;

                        // Drop icon (фиолетовая капля зелья).
            var dropGo = new GameObject("Drop");
            dropGo.transform.SetParent(innerGo.transform, false);
            var dropRt = dropGo.AddComponent<RectTransform>();
            dropRt.anchorMin = new Vector2(0.16f, 0.24f);
            dropRt.anchorMax = new Vector2(0.40f, 0.54f);
            dropRt.offsetMin = Vector2.zero;
            dropRt.offsetMax = Vector2.zero;
            var dropImg = dropGo.AddComponent<Image>();
            dropImg.sprite          = GetDropletSprite();
            dropImg.color           = new Color(0.7f, 0.4f, 0.95f);
            dropImg.preserveAspect  = true;

            // Cost (число справа от монетки).
            var costGo = new GameObject("Cost");
            costGo.transform.SetParent(innerGo.transform, false);
            var costRt = costGo.AddComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0.40f, 0.28f);
            costRt.anchorMax = new Vector2(0.85f, 0.50f);
            costRt.offsetMin = Vector2.zero;
            costRt.offsetMax = Vector2.zero;
            costLabel = costGo.AddComponent<TextMeshProUGUI>();
            costLabel.alignment        = TextAlignmentOptions.Left;
            costLabel.fontStyle        = FontStyles.Bold;
            costLabel.color            = new Color(1f, 0.95f, 0.5f);
            costLabel.enableAutoSizing = true;
            costLabel.fontSizeMin      = 24f;
            costLabel.fontSizeMax      = 64f;
            costLabel.outlineColor     = new Color32(0, 0, 0, 200);
            costLabel.outlineWidth     = 0.18f;

            // Bar BG.
            var barBgGo = new GameObject("BarBG");
            barBgGo.transform.SetParent(innerGo.transform, false);
            var barBgRt = barBgGo.AddComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.08f, 0.08f);
            barBgRt.anchorMax = new Vector2(0.92f, 0.20f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            var barBg = barBgGo.AddComponent<Image>();
            barBg.sprite = GetRoundedRectSprite();
            barBg.type   = Image.Type.Sliced;
            barBg.color  = new Color(0f, 0f, 0f, 0.6f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barBgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(3f, 3f);
            fillRt.offsetMax = new Vector2(-3f, -3f);
            barFill = fillGo.AddComponent<Image>();
            barFill.sprite     = GetRoundedRectSprite();
            barFill.type       = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.color      = new Color(0.45f, 0.85f, 0.45f);
            barFill.fillAmount = 0f;

            labelRoot.AddComponent<BillboardToCamera>();
        }
    }
}