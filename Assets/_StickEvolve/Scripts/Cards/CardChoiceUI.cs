using System;
using System.Collections.Generic;
using StickEvolve.Combat;
using StickEvolve.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.Cards
{
    /// <summary>
    /// Полноэкранное модальное окно с 3 кнопками-картами между волнами + ряд магазина внизу
    /// (реролл за золото, купить все 3 карты разом за крупную сумму).
    /// Создаётся целиком в коде — никаких префабов.
    /// </summary>
    public class CardChoiceUI : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private event Action<CardSO> _onPick;
        private readonly List<CardSO> _currentOptions = new();
        private readonly List<GameObject> _cardObjects = new();

        private Button _rerollBtn;
        private TextMeshProUGUI _rerollLabel;
        private Button _buyAllBtn;
        private TextMeshProUGUI _buyAllLabel;
        private TextMeshProUGUI _goldLabel;

        public event Action OnRerollClicked;
        public event Action OnBuyAllClicked;

        public bool IsOpen => _panel != null && _panel.activeSelf;
        public IReadOnlyList<CardSO> CurrentOptions => _currentOptions;

        public static CardChoiceUI Create(Canvas hudCanvas)
        {
            var go = new GameObject("CardChoiceUI", typeof(RectTransform));
            go.transform.SetParent(hudCanvas.transform, false);
            var ui = go.AddComponent<CardChoiceUI>();
            ui._canvas = hudCanvas;
            ui.BuildHidden();
            return ui;
        }

        private void BuildHidden()
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

            _panel.SetActive(false);
        }

        public void Show(List<CardSO> options, long gold, int rerollCost, int buyAllCost, Action<CardSO> onPick)
        {
            _onPick = onPick;
            _currentOptions.Clear();
            _currentOptions.AddRange(options);

            ClearPanelChildren();

            var titleBack = new GameObject("TitleBack", typeof(RectTransform));
            titleBack.transform.SetParent(_panel.transform, false);
            var titleBackRT = (RectTransform)titleBack.transform;
            titleBackRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleBackRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleBackRT.pivot = new Vector2(0.5f, 0.5f);
            titleBackRT.anchoredPosition = new Vector2(0f, 320f);
            titleBackRT.sizeDelta = new Vector2(680f, 74f);
            var titleBackImg = titleBack.AddComponent<Image>();
            titleBackImg.color = new Color(0.08f, 0.08f, 0.12f, 0.72f);
            titleBackImg.raycastTarget = false;

            var title = MakeText(_panel.transform, "Title", "ВЫБЕРИ КАРТУ", 56, new Vector2(0f, 320f), new Vector2(900f, 80f), TextAlignmentOptions.Center);
            title.color = Color.white;

            RebuildCards();
            BuildShopRow();
            RefreshShop(gold, rerollCost, buyAllCost);

            _panel.SetActive(true);
        }

        public void ReplaceCards(List<CardSO> options)
        {
            _currentOptions.Clear();
            _currentOptions.AddRange(options);
            RebuildCards();
        }

        public void RefreshShop(long gold, int rerollCost, int buyAllCost)
        {
            if (_rerollLabel != null) _rerollLabel.text = $"РЕРОЛЛ\n{rerollCost} g";
            if (_buyAllLabel != null) _buyAllLabel.text = $"ВЗЯТЬ ВСЕ 3\n{buyAllCost} g";
            if (_goldLabel != null) _goldLabel.text = $"GOLD: {gold}";
            if (_rerollBtn != null)
            {
                bool ok = gold >= rerollCost;
                _rerollBtn.interactable = ok;
                var img = _rerollBtn.targetGraphic as Image;
                if (img != null) img.color = ok ? new Color(0.32f, 0.55f, 0.85f) : new Color(0.3f, 0.3f, 0.3f);
            }
            if (_buyAllBtn != null)
            {
                bool ok = gold >= buyAllCost && _currentOptions.Count > 0;
                _buyAllBtn.interactable = ok;
                var img = _buyAllBtn.targetGraphic as Image;
                if (img != null) img.color = ok ? new Color(0.85f, 0.55f, 0.25f) : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void ClearPanelChildren()
        {
            _cardObjects.Clear();
            _rerollBtn = null;
            _rerollLabel = null;
            _buyAllBtn = null;
            _buyAllLabel = null;
            _goldLabel = null;
            for (int i = _panel.transform.childCount - 1; i >= 0; i--)
                Destroy(_panel.transform.GetChild(i).gameObject);
        }

        private void RebuildCards()
        {
            for (int i = _cardObjects.Count - 1; i >= 0; i--)
                if (_cardObjects[i] != null) Destroy(_cardObjects[i]);
            _cardObjects.Clear();

            float startX = -340f;
            float dx = 340f;
            for (int i = 0; i < _currentOptions.Count; i++)
            {
                var go = BuildCard(_currentOptions[i], new Vector2(startX + dx * i, 40f));
                _cardObjects.Add(go);
            }
        }

        private void BuildShopRow()
        {
            var row = new GameObject("ShopRow", typeof(RectTransform));
            row.transform.SetParent(_panel.transform, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = new Vector2(0.5f, 0.5f);
            rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(0f, -340f);
            rrt.sizeDelta = new Vector2(900f, 150f);

            _goldLabel = MakeText(row.transform, "Gold", "GOLD: 0", 30, new Vector2(0f, 70f), new Vector2(600f, 40f), TextAlignmentOptions.Center);
            _goldLabel.color = new Color(1f, 0.85f, 0.25f);

            _rerollBtn = MakeShopButton(row.transform, "Reroll", new Vector2(-200f, -20f), out _rerollLabel, new Color(0.32f, 0.55f, 0.85f));
            _rerollBtn.onClick.AddListener(() => OnRerollClicked?.Invoke());

            _buyAllBtn = MakeShopButton(row.transform, "BuyAll", new Vector2(200f, -20f), out _buyAllLabel, new Color(0.85f, 0.55f, 0.25f));
            _buyAllBtn.onClick.AddListener(() => OnBuyAllClicked?.Invoke());

            var hint = MakeText(row.transform, "Hint", "Стрелки / W S — двигать героев", 20, new Vector2(0f, -90f), new Vector2(900f, 30f), TextAlignmentOptions.Center);
            hint.color = new Color(0.7f, 0.7f, 0.75f);
        }

        private Button MakeShopButton(Transform parent, string name, Vector2 pos, out TextMeshProUGUI label, Color baseColor)
        {
            var go = new GameObject($"Btn_{name}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(280f, 90f);
            var img = go.AddComponent<Image>();
            img.color = baseColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            label = MakeText(go.transform, "Label", name, 24, Vector2.zero, new Vector2(280f, 80f), TextAlignmentOptions.Center);
            label.color = Color.white;
            label.raycastTarget = false;
            return btn;
        }

        private GameObject BuildCard(CardSO card, Vector2 anchoredPos)
        {
            var go = new GameObject($"Card_{card.id}", typeof(RectTransform));
            go.transform.SetParent(_panel.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(280f, 380f);

            var img = go.AddComponent<Image>();
            img.color = card.frameColor;

            var glow = new GameObject("Glow", typeof(RectTransform));
            glow.transform.SetParent(go.transform, false);
            var grt = (RectTransform)glow.transform;
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0f, 10f);
            grt.sizeDelta = new Vector2(250f, 250f);
            var glowImg = glow.AddComponent<Image>();
            glowImg.sprite = SpriteFactory.CardBurst();
            glowImg.color = new Color(card.frameColor.r, card.frameColor.g, card.frameColor.b, 0.22f);
            glowImg.raycastTarget = false;

            var inner = new GameObject("Inner", typeof(RectTransform));
            inner.transform.SetParent(go.transform, false);
            var irt = (RectTransform)inner.transform;
            irt.anchorMin = new Vector2(0f, 0f);
            irt.anchorMax = new Vector2(1f, 1f);
            irt.offsetMin = new Vector2(8f, 8f);
            irt.offsetMax = new Vector2(-8f, -8f);
            var iimg = inner.AddComponent<Image>();
            iimg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
            iimg.raycastTarget = false;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var card1 = card;
            btn.onClick.AddListener(() => OnPicked(card1));

            BuildCardIcon(go.transform, card);

            var name = MakeText(go.transform, "Name", card.displayName, 28, new Vector2(0f, 130f), new Vector2(260f, 60f), TextAlignmentOptions.Center);
            name.color = Color.white;

            var rarity = MakeText(go.transform, "Rarity", card.rarity.ToString().ToUpper(), 18, new Vector2(0f, 86f), new Vector2(260f, 30f), TextAlignmentOptions.Center);
            rarity.color = card.frameColor;

            var desc = MakeText(go.transform, "Desc", card.description, 22, new Vector2(0f, -100f), new Vector2(240f, 120f), TextAlignmentOptions.Center);
            desc.color = new Color(0.9f, 0.9f, 0.9f);

            int curLevel = CardProgression.GetLevel(card.id);
            if (curLevel > 0)
            {
                var badge = new GameObject("LvBadge", typeof(RectTransform));
                badge.transform.SetParent(go.transform, false);
                var brt = (RectTransform)badge.transform;
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(1f, 1f);
                brt.anchoredPosition = new Vector2(-12f, -12f);
                brt.sizeDelta = new Vector2(96f, 36f);
                var bImg = badge.AddComponent<Image>();
                bImg.color = new Color(0f, 0f, 0f, 0.55f);
                bImg.raycastTarget = false;

                var lvText = MakeText(badge.transform, "Lv", $"Lv {curLevel} → {curLevel + 1}", 18, Vector2.zero, new Vector2(96f, 36f), TextAlignmentOptions.Center);
                var lvRT = lvText.rectTransform;
                lvRT.anchorMin = Vector2.zero;
                lvRT.anchorMax = Vector2.one;
                lvRT.offsetMin = Vector2.zero;
                lvRT.offsetMax = Vector2.zero;
                lvText.color = new Color(1f, 0.9f, 0.4f);
            }
            return go;
        }

        private void BuildCardIcon(Transform parent, CardSO card)
        {
            var iconRoot = new GameObject("Icon", typeof(RectTransform));
            iconRoot.transform.SetParent(parent, false);
            var rt = (RectTransform)iconRoot.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 14f);
            rt.sizeDelta = new Vector2(120f, 120f);

            var disc = new GameObject("Disc", typeof(RectTransform));
            disc.transform.SetParent(iconRoot.transform, false);
            var drt = (RectTransform)disc.transform;
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(112f, 112f);
            var dimg = disc.AddComponent<Image>();
            dimg.sprite = SpriteFactory.SoftCircle();
            dimg.color = new Color(card.frameColor.r, card.frameColor.g, card.frameColor.b, 0.28f);
            dimg.raycastTarget = false;

            switch (card.effect)
            {
                case CardEffectKind.DamageMultiplier:
                    BuildSwordIcon(iconRoot.transform, card.frameColor);
                    break;
                case CardEffectKind.FireRateMultiplier:
                    BuildLightningIcon(iconRoot.transform);
                    break;
                case CardEffectKind.BulletSpeedAdd:
                    BuildBulletSpeedIcon(iconRoot.transform);
                    break;
                case CardEffectKind.RangeAdd:
                    BuildEyeRangeIcon(iconRoot.transform);
                    break;
                case CardEffectKind.MaxHpMultiplier:
                    BuildHeartIcon(iconRoot.transform);
                    break;
                case CardEffectKind.ThornsAdd:
                    BuildThornsShieldIcon(iconRoot.transform);
                    break;
                case CardEffectKind.CritChanceAdd:
                case CardEffectKind.CritMultiplierAdd:
                    BuildCritIcon(iconRoot.transform);
                    break;
                case CardEffectKind.MultiShotAdd:
                    BuildMultiShotIcon(iconRoot.transform);
                    break;
                case CardEffectKind.BulletPierceAdd:
                    BuildPierceIcon(iconRoot.transform);
                    break;
                case CardEffectKind.FullHeal:
                    BuildHealIcon(iconRoot.transform);
                    break;
                case CardEffectKind.LifestealAdd:
                    BuildLifestealIcon(iconRoot.transform);
                    break;
                case CardEffectKind.SpawnExtraHero:
                    BuildSquadIcon(iconRoot.transform, card.frameColor);
                    break;
                case CardEffectKind.SpawnHeroOfClass:
                    BuildHeroClassIcon(iconRoot.transform, card);
                    break;
                case CardEffectKind.GoldGainMult:
                    BuildGoldIcon(iconRoot.transform);
                    break;
            }
        }

        private void BuildSwordIcon(Transform parent, Color frame)
        {
            AddIconRect(parent, "Blade", new Color(0.90f, 0.92f, 1f), new Vector2(0f, 10f), new Vector2(18f, 70f), 0f);
            AddIconRect(parent, "BladeShine", new Color(1f, 1f, 1f, 0.55f), new Vector2(4f, 12f), new Vector2(4f, 58f), 0f);
            AddIconShape(parent, "Tip", SpriteFactory.Triangle(), new Color(0.90f, 0.92f, 1f), new Vector2(0f, 52f), new Vector2(22f, 24f));
            AddIconRect(parent, "Guard", new Color(1f, 0.78f, 0.20f), new Vector2(0f, -28f), new Vector2(56f, 9f), 0f);
            AddIconRect(parent, "Handle", frame, new Vector2(0f, -48f), new Vector2(12f, 34f), 0f);
            AddIconShape(parent, "Pommel", SpriteFactory.Circle(), new Color(1f, 0.78f, 0.20f), new Vector2(0f, -66f), new Vector2(18f, 18f));
        }

        private void BuildLightningIcon(Transform parent)
        {
            AddIconShape(parent, "BoltTop", SpriteFactory.Triangle(), new Color(1f, 0.90f, 0.18f), new Vector2(-8f, 18f), new Vector2(50f, 54f), 210f);
            AddIconShape(parent, "BoltBottom", SpriteFactory.Triangle(), new Color(1f, 0.72f, 0.12f), new Vector2(8f, -20f), new Vector2(48f, 58f), 30f);
            AddIconText(parent, "Rate", "×2", 22, new Vector2(34f, -34f), new Vector2(52f, 30f), new Color(1f, 0.95f, 0.60f));
        }

        private void BuildBulletSpeedIcon(Transform parent)
        {
            AddIconShape(parent, "Bullet", SpriteFactory.Circle(), new Color(0.62f, 0.90f, 1f), new Vector2(-20f, 0f), new Vector2(30f, 30f));
            AddIconShape(parent, "Nose", SpriteFactory.Triangle(), new Color(0.62f, 0.90f, 1f), new Vector2(0f, 0f), new Vector2(28f, 28f), -90f);
            AddIconRect(parent, "TrailA", new Color(0.45f, 0.75f, 1f, 0.9f), new Vector2(28f, 0f), new Vector2(54f, 9f), 0f);
            AddIconRect(parent, "TrailB", new Color(0.90f, 0.95f, 1f, 0.65f), new Vector2(32f, 16f), new Vector2(36f, 6f), 0f);
            AddIconRect(parent, "TrailC", new Color(0.90f, 0.95f, 1f, 0.65f), new Vector2(32f, -16f), new Vector2(36f, 6f), 0f);
        }

        private void BuildEyeRangeIcon(Transform parent)
        {
            AddIconShape(parent, "EyeOuter", SpriteFactory.SoftCircle(), new Color(0.82f, 0.92f, 1f), Vector2.zero, new Vector2(96f, 56f));
            AddIconShape(parent, "EyeIris", SpriteFactory.Circle(), new Color(0.25f, 0.55f, 1f), Vector2.zero, new Vector2(34f, 34f));
            AddIconShape(parent, "EyePupil", SpriteFactory.Circle(), new Color(0.04f, 0.05f, 0.08f), Vector2.zero, new Vector2(16f, 16f));
            AddIconRect(parent, "RangeLine", new Color(0.85f, 0.95f, 1f, 0.75f), new Vector2(0f, -42f), new Vector2(72f, 5f), 0f);
            AddIconText(parent, "PlusRange", "+", 26, new Vector2(42f, -42f), new Vector2(30f, 30f), new Color(0.85f, 0.95f, 1f));
        }

        private void BuildHeartIcon(Transform parent)
        {
            var red = new Color(1f, 0.22f, 0.34f);
            AddIconShape(parent, "HeartLobeL", SpriteFactory.Circle(), red, new Vector2(-12f, 12f), new Vector2(34f, 34f));
            AddIconShape(parent, "HeartLobeR", SpriteFactory.Circle(), red, new Vector2(12f, 12f), new Vector2(34f, 34f));
            AddIconRect(parent, "HeartBodyL", red, new Vector2(-10f, -10f), new Vector2(30f, 38f), 45f);
            AddIconRect(parent, "HeartBodyR", red, new Vector2(10f, -10f), new Vector2(30f, 38f), -45f);
            AddIconShape(parent, "HeartTip", SpriteFactory.Triangle(), red, new Vector2(0f, -34f), new Vector2(34f, 28f), 180f);
        }

        private void BuildThornsShieldIcon(Transform parent)
        {
            AddIconShape(parent, "Shield", SpriteFactory.SoftCircle(), new Color(0.30f, 0.86f, 0.88f), Vector2.zero, new Vector2(74f, 88f));
            AddIconRect(parent, "ShieldStripe", new Color(0.90f, 1f, 1f), Vector2.zero, new Vector2(12f, 68f), 0f);
            AddIconShape(parent, "ThornL", SpriteFactory.Triangle(), new Color(0.92f, 0.98f, 1f), new Vector2(-42f, 0f), new Vector2(28f, 28f), 90f);
            AddIconShape(parent, "ThornR", SpriteFactory.Triangle(), new Color(0.92f, 0.98f, 1f), new Vector2(42f, 0f), new Vector2(28f, 28f), -90f);
            AddIconShape(parent, "ThornT", SpriteFactory.Triangle(), new Color(0.92f, 0.98f, 1f), new Vector2(0f, 48f), new Vector2(24f, 24f), 0f);
        }

        private void BuildCritIcon(Transform parent)
        {
            BuildSwordIcon(parent, new Color(1f, 0.92f, 0.25f));
            AddIconShape(parent, "CritStar", SpriteFactory.Spark(), new Color(1f, 0.92f, 0.20f), new Vector2(30f, 30f), new Vector2(40f, 40f));
            AddIconText(parent, "CritBang", "!", 30, new Vector2(30f, 30f), new Vector2(36f, 36f), new Color(0.35f, 0.18f, 0.02f));
        }

        private void BuildMultiShotIcon(Transform parent)
        {
            AddIconRect(parent, "ArrowA", new Color(0.92f, 0.92f, 0.82f), new Vector2(0f, 22f), new Vector2(72f, 7f), 0f);
            AddIconRect(parent, "ArrowB", new Color(0.92f, 0.92f, 0.82f), new Vector2(0f, 0f), new Vector2(72f, 7f), 0f);
            AddIconRect(parent, "ArrowC", new Color(0.92f, 0.92f, 0.82f), new Vector2(0f, -22f), new Vector2(72f, 7f), 0f);
            AddIconShape(parent, "HeadA", SpriteFactory.Triangle(), new Color(0.92f, 0.92f, 0.82f), new Vector2(42f, 22f), new Vector2(20f, 20f), -90f);
            AddIconShape(parent, "HeadB", SpriteFactory.Triangle(), new Color(0.92f, 0.92f, 0.82f), new Vector2(42f, 0f), new Vector2(20f, 20f), -90f);
            AddIconShape(parent, "HeadC", SpriteFactory.Triangle(), new Color(0.92f, 0.92f, 0.82f), new Vector2(42f, -22f), new Vector2(20f, 20f), -90f);
        }

        private void BuildPierceIcon(Transform parent)
        {
            AddIconRect(parent, "Arrow", new Color(0.92f, 0.92f, 0.82f), new Vector2(0f, 0f), new Vector2(92f, 8f), 0f);
            AddIconShape(parent, "ArrowHead", SpriteFactory.Triangle(), new Color(0.92f, 0.92f, 0.82f), new Vector2(50f, 0f), new Vector2(24f, 24f), -90f);
            AddIconShape(parent, "TargetA", SpriteFactory.Circle(), new Color(0.95f, 0.30f, 0.35f, 0.85f), new Vector2(-26f, 0f), new Vector2(34f, 34f));
            AddIconShape(parent, "TargetB", SpriteFactory.Circle(), new Color(0.95f, 0.30f, 0.35f, 0.85f), new Vector2(18f, 0f), new Vector2(34f, 34f));
        }

        private void BuildHealIcon(Transform parent)
        {
            AddIconRect(parent, "PlusV", new Color(0.50f, 1f, 0.60f), Vector2.zero, new Vector2(28f, 86f), 0f);
            AddIconRect(parent, "PlusH", new Color(0.50f, 1f, 0.60f), Vector2.zero, new Vector2(86f, 28f), 0f);
            AddIconShape(parent, "HealSpark", SpriteFactory.Spark(), new Color(0.90f, 1f, 0.80f), new Vector2(38f, 34f), new Vector2(30f, 30f));
        }

        private void BuildLifestealIcon(Transform parent)
        {
            var blood = new Color(0.82f, 0.02f, 0.08f);
            AddIconShape(parent, "BloodRound", SpriteFactory.Circle(), blood, new Vector2(0f, -2f), new Vector2(50f, 58f));
            AddIconShape(parent, "BloodPoint", SpriteFactory.Triangle(), blood, new Vector2(0f, -46f), new Vector2(44f, 38f), 180f);
            AddIconShape(parent, "BloodHighlight", SpriteFactory.Circle(), new Color(1f, 0.36f, 0.40f, 0.85f), new Vector2(-12f, 10f), new Vector2(13f, 18f));
        }

        private void BuildSquadIcon(Transform parent, Color frame)
        {
            AddMiniPerson(parent, "HeroA", new Vector2(-30f, -4f), new Color(0.60f, 0.85f, 1f));
            AddMiniPerson(parent, "HeroB", new Vector2(0f, 8f), frame);
            AddMiniPerson(parent, "HeroC", new Vector2(30f, -4f), new Color(0.65f, 1f, 0.65f));
            AddIconText(parent, "Plus", "+", 34, new Vector2(42f, 36f), new Vector2(36f, 36f), new Color(1f, 0.94f, 0.35f));
        }

        private void BuildHeroClassIcon(Transform parent, CardSO card)
        {
            BuildSquadIcon(parent, card.frameColor);
            var cls = (HeroClass)Mathf.RoundToInt(card.secondaryValue);
            switch (cls)
            {
                case HeroClass.Archer:
                    AddIconRect(parent, "ClassBow", new Color(0.45f, 0.26f, 0.10f), new Vector2(-42f, 4f), new Vector2(8f, 56f), -18f);
                    break;
                case HeroClass.Mage:
                case HeroClass.Healer:
                    AddIconRect(parent, "ClassStaff", new Color(0.62f, 0.40f, 0.16f), new Vector2(-42f, 0f), new Vector2(7f, 58f), 0f);
                    AddIconShape(parent, "ClassOrb", SpriteFactory.Spark(), card.frameColor, new Vector2(-42f, 34f), new Vector2(24f, 24f));
                    break;
                case HeroClass.Tank:
                    AddIconShape(parent, "ClassShield", SpriteFactory.SoftCircle(), new Color(1f, 0.70f, 0.30f), new Vector2(-42f, 0f), new Vector2(28f, 36f));
                    break;
                case HeroClass.Berserker:
                    AddIconRect(parent, "ClassAxe", new Color(0.35f, 0.18f, 0.08f), new Vector2(-42f, 0f), new Vector2(7f, 58f), -25f);
                    AddIconShape(parent, "ClassAxeHead", SpriteFactory.Triangle(), new Color(0.85f, 0.85f, 0.90f), new Vector2(-30f, 24f), new Vector2(24f, 24f), 90f);
                    break;
                case HeroClass.Sniper:
                    AddIconRect(parent, "ClassRifle", new Color(0.12f, 0.14f, 0.16f), new Vector2(-42f, 0f), new Vector2(58f, 8f), 0f);
                    break;
                case HeroClass.Ninja:
                    AddIconShape(parent, "ClassMask", SpriteFactory.SoftCircle(), new Color(0.05f, 0.06f, 0.08f), new Vector2(-42f, 8f), new Vector2(30f, 18f));
                    break;
            }
        }

        private void BuildGoldIcon(Transform parent)
        {
            AddIconShape(parent, "CoinA", SpriteFactory.Circle(), new Color(1f, 0.78f, 0.18f), new Vector2(-20f, -8f), new Vector2(54f, 54f));
            AddIconShape(parent, "CoinB", SpriteFactory.Circle(), new Color(1f, 0.88f, 0.28f), new Vector2(18f, 12f), new Vector2(54f, 54f));
            AddIconText(parent, "G", "G", 34, new Vector2(18f, 12f), new Vector2(54f, 54f), new Color(0.45f, 0.25f, 0.04f));
            AddIconText(parent, "Up", "↑", 30, new Vector2(42f, -32f), new Vector2(36f, 36f), new Color(1f, 0.95f, 0.45f));
        }

        private void AddMiniPerson(Transform parent, string name, Vector2 pos, Color color)
        {
            AddIconShape(parent, $"{name}_Head", SpriteFactory.Circle(), new Color(0.82f, 0.92f, 1f), pos + new Vector2(0f, 22f), new Vector2(22f, 22f));
            AddIconRect(parent, $"{name}_Body", color, pos + new Vector2(0f, -8f), new Vector2(20f, 42f), 0f);
            AddIconRect(parent, $"{name}_ArmL", color, pos + new Vector2(-16f, -6f), new Vector2(7f, 32f), -22f);
            AddIconRect(parent, $"{name}_ArmR", color, pos + new Vector2(16f, -6f), new Vector2(7f, 32f), 22f);
        }

        private void AddIconRect(Transform parent, string name, Color color, Vector2 pos, Vector2 size, float angle)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void AddIconShape(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size, float angle = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
        }

        private void AddIconText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 sizeDelta, Color color)
        {
            var tmp = MakeText(parent, name, text, size, pos, sizeDelta, TextAlignmentOptions.Center);
            tmp.color = color;
            tmp.raycastTarget = false;
        }

        private TextMeshProUGUI MakeText(Transform parent, string label, string text, float fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject($"Text_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
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
