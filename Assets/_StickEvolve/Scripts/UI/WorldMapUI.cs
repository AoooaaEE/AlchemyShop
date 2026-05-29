using StickEvolve.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Карта мира: 4 биома × 10 уровней. Показывает звёзды на пройденных,
    /// замок на закрытых. Клик на разблокированный уровень — запуск.
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _content;
        private System.Action<int> _onLevelChosen;
        private System.Action _onClose;

        private static readonly string[] BiomeNamesUI =
        {
            "ЛЕС", "СНЕЖНЫЕ ГОРЫ", "ПУСТЫНЯ", "ЗАМОК"
        };
        private static readonly Color[] BiomeColors =
        {
            new Color(0.30f, 0.62f, 0.30f),
            new Color(0.55f, 0.78f, 0.95f),
            new Color(0.95f, 0.78f, 0.35f),
            new Color(0.55f, 0.30f, 0.62f),
        };

        public static WorldMapUI Create(Canvas canvas, System.Action<int> onLevelChosen, System.Action onClose)
        {
            var go = new GameObject("WorldMapUI");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var ui = go.AddComponent<WorldMapUI>();
            ui._onLevelChosen = onLevelChosen;
            ui._onClose = onClose;
            ui.Build();
            ui._root.SetActive(false);
            return ui;
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Show()
        {
            RebuildLevelButtons();
            _root.SetActive(true);
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        private void Build()
        {
            _root = new GameObject("Root");
            _root.transform.SetParent(transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Затемнение фона
            var bg = new GameObject("BG");
            bg.transform.SetParent(_root.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
            bgImg.raycastTarget = true;

            // Заголовок
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_root.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -40f);
            titleRT.sizeDelta = new Vector2(800f, 80f);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "КАРТА МИРА";
            titleText.fontSize = 64;
            titleText.color = new Color(1f, 0.9f, 0.4f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Кнопка закрытия (X)
            var closeGO = new GameObject("CloseBtn");
            closeGO.transform.SetParent(_root.transform, false);
            var closeRT = closeGO.AddComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 1f);
            closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(1f, 1f);
            closeRT.anchoredPosition = new Vector2(-30f, -30f);
            closeRT.sizeDelta = new Vector2(80f, 80f);
            var closeImg = closeGO.AddComponent<Image>();
            closeImg.color = new Color(0.7f, 0.2f, 0.2f);
            var closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() =>
            {
                Hide();
                _onClose?.Invoke();
            });

            var closeTGO = new GameObject("X");
            closeTGO.transform.SetParent(closeGO.transform, false);
            var closeTRT = closeTGO.AddComponent<RectTransform>();
            closeTRT.anchorMin = Vector2.zero;
            closeTRT.anchorMax = Vector2.one;
            closeTRT.offsetMin = Vector2.zero;
            closeTRT.offsetMax = Vector2.zero;
            var closeText = closeTGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 48;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.fontStyle = FontStyles.Bold;

            // Контейнер скролла
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(_root.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(40f, 60f);
            scrollRT.offsetMax = new Vector2(-40f, -140f);
            var scrollBgImg = scrollGO.AddComponent<Image>();
            scrollBgImg.color = new Color(0f, 0f, 0f, 0.3f);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(scrollGO.transform, false);
            var vpRT = vpGO.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            var vpImg = vpGO.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.01f);
            var vpMask = vpGO.AddComponent<Mask>();
            vpMask.showMaskGraphic = false;
            scrollRect.viewport = vpRT;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(vpGO.transform, false);
            _content = contentGO.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;
            _content.sizeDelta = new Vector2(0f, 100f);
            scrollRect.content = _content;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 30;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RebuildLevelButtons()
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
                Destroy(_content.GetChild(i).gameObject);

            for (int biome = 0; biome < CampaignBuilder.BiomeCount; biome++)
                BuildBiomeSection(biome);
        }

        private void BuildBiomeSection(int biomeIdx)
        {
            var headerGO = new GameObject($"BiomeHeader_{biomeIdx}");
            headerGO.transform.SetParent(_content, false);
            headerGO.AddComponent<RectTransform>();
            var headerLE = headerGO.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 70;
            var headerImg = headerGO.AddComponent<Image>();
            headerImg.color = BiomeColors[biomeIdx];

            var headerTGO = new GameObject("Text");
            headerTGO.transform.SetParent(headerGO.transform, false);
            var htRT = headerTGO.AddComponent<RectTransform>();
            htRT.anchorMin = Vector2.zero;
            htRT.anchorMax = Vector2.one;
            htRT.offsetMin = Vector2.zero;
            htRT.offsetMax = Vector2.zero;
            var headerText = headerTGO.AddComponent<TextMeshProUGUI>();
            headerText.text = BiomeNamesUI[biomeIdx];
            headerText.fontSize = 40;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;

            var gridGO = new GameObject($"BiomeGrid_{biomeIdx}");
            gridGO.transform.SetParent(_content, false);
            gridGO.AddComponent<RectTransform>();
            var gridLE = gridGO.AddComponent<LayoutElement>();
            gridLE.preferredHeight = 320;
            var glg = gridGO.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(180f, 140f);
            glg.spacing = new Vector2(15f, 15f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 5;
            glg.childAlignment = TextAnchor.MiddleCenter;

            int startLevel = biomeIdx * CampaignBuilder.LevelsPerBiome + 1;
            for (int i = 0; i < CampaignBuilder.LevelsPerBiome; i++)
            {
                int levelNumber = startLevel + i;
                CreateLevelButton(gridGO.transform, levelNumber);
            }
        }

        private void CreateLevelButton(Transform parent, int levelNumber)
        {
            var btnGO = new GameObject($"Level_{levelNumber}");
            btnGO.transform.SetParent(parent, false);
            btnGO.AddComponent<RectTransform>();

            bool unlocked = LevelProgress.IsUnlocked(levelNumber);
            bool completed = LevelProgress.IsCompleted(levelNumber);
            int stars = LevelProgress.GetStars(levelNumber);

            var img = btnGO.AddComponent<Image>();
            if (!unlocked)
                img.color = new Color(0.25f, 0.25f, 0.30f);
            else if (completed)
                img.color = new Color(0.30f, 0.65f, 0.35f);
            else
                img.color = new Color(0.95f, 0.75f, 0.20f);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = img;
            if (unlocked)
            {
                int captured = levelNumber;
                btn.onClick.AddListener(() =>
                {
                    _onLevelChosen?.Invoke(captured);
                });
            }
            else
            {
                btn.interactable = false;
            }

            var numGO = new GameObject("Num");
            numGO.transform.SetParent(btnGO.transform, false);
            var numRT = numGO.AddComponent<RectTransform>();
            numRT.anchorMin = new Vector2(0f, 0.4f);
            numRT.anchorMax = new Vector2(1f, 1f);
            numRT.offsetMin = Vector2.zero;
            numRT.offsetMax = Vector2.zero;
            var numText = numGO.AddComponent<TextMeshProUGUI>();
            numText.text = levelNumber.ToString();
            numText.fontSize = 48;
            numText.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.55f);
            numText.alignment = TextAlignmentOptions.Center;
            numText.fontStyle = FontStyles.Bold;

            var starsGO = new GameObject("Stars");
            starsGO.transform.SetParent(btnGO.transform, false);
            var starsRT = starsGO.AddComponent<RectTransform>();
            starsRT.anchorMin = new Vector2(0f, 0f);
            starsRT.anchorMax = new Vector2(1f, 0.4f);
            starsRT.offsetMin = Vector2.zero;
            starsRT.offsetMax = Vector2.zero;
            var starsText = starsGO.AddComponent<TextMeshProUGUI>();
            string starsLine = "";
            if (stars > 0)
            {
                for (int i = 0; i < stars; i++) starsLine += "\u2605";
                for (int i = stars; i < 3; i++) starsLine += "\u2606";
            }
            else if (!unlocked)
            {
                starsLine = "---";
            }
            starsText.text = starsLine;
            starsText.fontSize = 30;
            starsText.color = new Color(1f, 0.95f, 0.4f);
            starsText.alignment = TextAlignmentOptions.Center;
        }
    }
}
