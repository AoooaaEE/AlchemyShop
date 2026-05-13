using System;
using System.Collections.Generic;
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

            var name = MakeText(go.transform, "Name", card.displayName, 28, new Vector2(0f, 130f), new Vector2(260f, 60f), TextAlignmentOptions.Center);
            name.color = Color.white;

            var rarity = MakeText(go.transform, "Rarity", card.rarity.ToString().ToUpper(), 18, new Vector2(0f, 80f), new Vector2(260f, 30f), TextAlignmentOptions.Center);
            rarity.color = card.frameColor;

            var desc = MakeText(go.transform, "Desc", card.description, 22, new Vector2(0f, -80f), new Vector2(240f, 120f), TextAlignmentOptions.Center);
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
