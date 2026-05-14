using System;
using System.Collections.Generic;
using StickEvolve.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Магазин мета-апгрейдов (вне раннов). Открывается из главного меню.
    /// Тратит камни эволюции (мета-валюта). Каждый купленный уровень повышает
    /// "ascension" — общую сложность волн (см. MetaProgression).
    /// </summary>
    public class MetaShopUI : MonoBehaviour
    {
        private GameObject _panel;
        private Action _onClose;
        private TextMeshProUGUI _gemsLabel;
        private TextMeshProUGUI _ascensionLabel;
        private readonly List<UpgradeRow> _rows = new();

        private class UpgradeRow
        {
            public MetaUpgradeDef def;
            public TextMeshProUGUI levelLabel;
            public TextMeshProUGUI costLabel;
            public Image buttonImg;
            public Button button;
        }

        public static MetaShopUI Create(Canvas canvas, Action onClose)
        {
            var go = new GameObject("MetaShopUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var ui = go.AddComponent<MetaShopUI>();
            ui._onClose = onClose;
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
            bg.color = new Color(0.02f, 0.04f, 0.10f, 0.95f);

            // Заголовок
            var title = MakeText(_panel.transform, "Title", "МАГАЗИН АПГРЕЙДОВ", 64, new Vector2(0f, -80f), new Vector2(1000f, 80f), TextAlignmentOptions.Center);
            title.color = new Color(1f, 0.92f, 0.45f);
            title.fontStyle = FontStyles.Bold;
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            _gemsLabel = MakeText(_panel.transform, "Gems", "Камни: 0", 36, new Vector2(0f, -150f), new Vector2(800f, 50f), TextAlignmentOptions.Center);
            _gemsLabel.color = new Color(0.85f, 0.55f, 1f);
            _gemsLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _gemsLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            _ascensionLabel = MakeText(_panel.transform, "Asc", "Ascension: +0% HP / +0% DMG врагам", 22, new Vector2(0f, -190f), new Vector2(1000f, 32f), TextAlignmentOptions.Center);
            _ascensionLabel.color = new Color(1f, 0.7f, 0.4f, 0.95f);
            _ascensionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _ascensionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);

            // ScrollView с апгрейдами
            var scroll = new GameObject("Scroll", typeof(RectTransform));
            scroll.transform.SetParent(_panel.transform, false);
            var scRT = (RectTransform)scroll.transform;
            scRT.anchorMin = new Vector2(0.5f, 0f);
            scRT.anchorMax = new Vector2(0.5f, 1f);
            scRT.pivot = new Vector2(0.5f, 0.5f);
            scRT.sizeDelta = new Vector2(900f, 0f);
            scRT.offsetMin = new Vector2(-450f, 150f);
            scRT.offsetMax = new Vector2(450f, -240f);
            var scrollImg = scroll.AddComponent<Image>();
            scrollImg.color = new Color(0.1f, 0.12f, 0.18f, 0.9f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(scroll.transform, false);
            var coRT = (RectTransform)content.transform;
            coRT.anchorMin = new Vector2(0f, 1f);
            coRT.anchorMax = new Vector2(1f, 1f);
            coRT.pivot = new Vector2(0.5f, 1f);
            coRT.offsetMin = new Vector2(8f, -1100f);
            coRT.offsetMax = new Vector2(-8f, -8f);

            float y = 0f;
            float rowH = 120f;
            foreach (var def in MetaProgression.Catalog)
            {
                BuildRow(content.transform, def, y, rowH);
                y -= rowH + 8f;
            }

            // Кнопка "Назад"
            var backGO = new GameObject("BackBtn", typeof(RectTransform));
            backGO.transform.SetParent(_panel.transform, false);
            var bRT = (RectTransform)backGO.transform;
            bRT.anchorMin = new Vector2(0.5f, 0f);
            bRT.anchorMax = new Vector2(0.5f, 0f);
            bRT.pivot = new Vector2(0.5f, 0f);
            bRT.anchoredPosition = new Vector2(0f, 30f);
            bRT.sizeDelta = new Vector2(360f, 90f);
            var bImg = backGO.AddComponent<Image>();
            bImg.color = new Color(0.45f, 0.5f, 0.6f);
            var bBtn = backGO.AddComponent<Button>();
            bBtn.targetGraphic = bImg;
            bBtn.onClick.AddListener(() => _onClose?.Invoke());
            var bLabel = MakeText(backGO.transform, "Label", "НАЗАД", 32, Vector2.zero, new Vector2(340f, 80f), TextAlignmentOptions.Center);
            bLabel.color = Color.white;
            bLabel.raycastTarget = false;
            bLabel.fontStyle = FontStyles.Bold;
        }

        private void BuildRow(Transform parent, MetaUpgradeDef def, float yOffset, float rowH)
        {
            var rowGO = new GameObject($"Row_{def.id}", typeof(RectTransform));
            rowGO.transform.SetParent(parent, false);
            var rRT = (RectTransform)rowGO.transform;
            rRT.anchorMin = new Vector2(0f, 1f);
            rRT.anchorMax = new Vector2(1f, 1f);
            rRT.pivot = new Vector2(0.5f, 1f);
            rRT.anchoredPosition = new Vector2(0f, yOffset);
            rRT.sizeDelta = new Vector2(-16f, rowH);
            var bg = rowGO.AddComponent<Image>();
            bg.color = new Color(def.color.r * 0.25f, def.color.g * 0.25f, def.color.b * 0.25f, 0.95f);

            // Цветная полоска слева
            var bar = new GameObject("Bar", typeof(RectTransform));
            bar.transform.SetParent(rowGO.transform, false);
            var barRT = (RectTransform)bar.transform;
            barRT.anchorMin = new Vector2(0f, 0f);
            barRT.anchorMax = new Vector2(0f, 1f);
            barRT.pivot = new Vector2(0f, 0.5f);
            barRT.offsetMin = new Vector2(0f, 6f);
            barRT.offsetMax = new Vector2(10f, -6f);
            bar.AddComponent<Image>().color = def.color;

            var nameLbl = MakeText(rowGO.transform, "Name", def.displayName, 30, new Vector2(150f, 25f), new Vector2(440f, 36f), TextAlignmentOptions.Left);
            nameLbl.fontStyle = FontStyles.Bold;
            nameLbl.color = Color.white;
            nameLbl.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            nameLbl.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            nameLbl.rectTransform.pivot = new Vector2(0f, 0.5f);

            var descLbl = MakeText(rowGO.transform, "Desc", def.description, 18, new Vector2(150f, -10f), new Vector2(440f, 50f), TextAlignmentOptions.Left);
            descLbl.color = new Color(0.85f, 0.85f, 0.85f);
            descLbl.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            descLbl.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            descLbl.rectTransform.pivot = new Vector2(0f, 0.5f);
            descLbl.textWrappingMode = TextWrappingModes.Normal;

            var levelLbl = MakeText(rowGO.transform, "Level", "0/0", 26, new Vector2(-280f, 0f), new Vector2(120f, 36f), TextAlignmentOptions.Center);
            levelLbl.color = new Color(0.95f, 0.95f, 0.55f);
            levelLbl.fontStyle = FontStyles.Bold;
            levelLbl.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            levelLbl.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            levelLbl.rectTransform.pivot = new Vector2(1f, 0.5f);

            // Кнопка "Купить"
            var btnGO = new GameObject("BuyBtn", typeof(RectTransform));
            btnGO.transform.SetParent(rowGO.transform, false);
            var bRT = (RectTransform)btnGO.transform;
            bRT.anchorMin = new Vector2(1f, 0.5f);
            bRT.anchorMax = new Vector2(1f, 0.5f);
            bRT.pivot = new Vector2(1f, 0.5f);
            bRT.anchoredPosition = new Vector2(-10f, 0f);
            bRT.sizeDelta = new Vector2(150f, 80f);
            var bImg = btnGO.AddComponent<Image>();
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bImg;
            var costLbl = MakeText(btnGO.transform, "Cost", "10", 28, Vector2.zero, new Vector2(150f, 80f), TextAlignmentOptions.Center);
            costLbl.color = Color.white;
            costLbl.raycastTarget = false;
            costLbl.fontStyle = FontStyles.Bold;

            var row = new UpgradeRow { def = def, levelLabel = levelLbl, costLabel = costLbl, buttonImg = bImg, button = btn };
            btn.onClick.AddListener(() => { if (MetaProgression.TryBuy(def.id)) { Refresh(); StickEvolve.Core.StickGame.Instance?.PersistSave(); } });
            _rows.Add(row);
        }

        public void Show()
        {
            _panel.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            _panel.SetActive(false);
        }

        private void Refresh()
        {
            _gemsLabel.text = $"Камней эволюции: {MetaProgression.MetaGems}";
            float hpMult = MetaProgression.EnemyHpAscensionMult();
            float dmgMult = MetaProgression.EnemyDamageAscensionMult();
            int hpPct = Mathf.RoundToInt((hpMult - 1f) * 100f);
            int dmgPct = Mathf.RoundToInt((dmgMult - 1f) * 100f);
            _ascensionLabel.text = $"Ascension: +{hpPct}% HP / +{dmgPct}% DMG врагам (компенсация прокачки)";

            foreach (var row in _rows)
            {
                int level = MetaProgression.GetLevel(row.def.id);
                row.levelLabel.text = $"Ур. {level}/{row.def.maxLevel}";

                int nextCost = MetaProgression.NextCost(row.def.id);
                if (nextCost < 0)
                {
                    row.costLabel.text = "MAX";
                    row.buttonImg.color = new Color(0.3f, 0.3f, 0.3f);
                    row.button.interactable = false;
                }
                else
                {
                    row.costLabel.text = $"{nextCost} камн.";
                    bool canBuy = MetaProgression.MetaGems >= nextCost;
                    row.buttonImg.color = canBuy ? new Color(0.35f, 0.7f, 0.45f) : new Color(0.55f, 0.3f, 0.3f);
                    row.button.interactable = canBuy;
                }
            }
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
