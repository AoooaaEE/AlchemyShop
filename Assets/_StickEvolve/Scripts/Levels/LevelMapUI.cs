using System;
using System.Collections.Generic;
using StickEvolve.Core;
using StickEvolve.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.Levels
{
    /// <summary>
    /// Full-screen level selection map. Shows biome zones (Plains, Ice) with level nodes.
    /// Completed levels are colored, current level pulses, locked levels are grey.
    /// Clicking an unlocked level starts gameplay.
    /// </summary>
    public class LevelMapUI : MonoBehaviour
    {
        public event Action<int> OnLevelSelected;

        private Canvas _canvas;
        private GameObject _root;
        private ScrollRect _scroll;
        private RectTransform _contentRT;
        private readonly List<LevelNode> _nodes = new();
        private TextMeshProUGUI _titleText;

        private int _highestUnlocked;

        private struct LevelNode
        {
            public int level;
            public Button button;
            public Image icon;
            public TextMeshProUGUI label;
            public Image lockIcon;
        }

        public static LevelMapUI Create(Canvas canvas)
        {
            var go = new GameObject("LevelMapUI");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var map = go.AddComponent<LevelMapUI>();
            map._canvas = canvas;
            map._root = go;
            map.BuildLayout();
            go.SetActive(false);
            return map;
        }

        private void BuildLayout()
        {
            // Full-screen dark background
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.97f);

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_root.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, -20f);
            titleRT.sizeDelta = new Vector2(0f, 100f);
            _titleText = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.text = "КАРТА УРОВНЕЙ";
            _titleText.fontSize = 52;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color = new Color(1f, 0.92f, 0.6f);

            // Scrollable content area
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(_root.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(20f, 20f);
            scrollRT.offsetMax = new Vector2(-20f, -130f);
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
            scrollGO.AddComponent<Mask>();

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            _contentRT = contentGO.AddComponent<RectTransform>();
            _contentRT.anchorMin = new Vector2(0f, 0f);
            _contentRT.anchorMax = new Vector2(1f, 0f);
            _contentRT.pivot = new Vector2(0.5f, 0f);

            _scroll = scrollGO.AddComponent<ScrollRect>();
            _scroll.content = _contentRT;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic;

            BuildBiomeZones();
        }

        private void BuildBiomeZones()
        {
            float nodeHeight = 130f;
            float biomeHeaderHeight = 80f;
            float spacing = 15f;

            float totalHeight = 0f;

            // Plains biome zone
            totalHeight += BuildBiomeHeader("РАВНИНЫ", new Color(0.42f, 0.66f, 0.28f), totalHeight);

            for (int lvl = 1; lvl <= LevelCatalog.LevelsPerBiome; lvl++)
            {
                totalHeight += spacing;
                BuildLevelNode(lvl, BiomeType.Plains, totalHeight, nodeHeight);
                totalHeight += nodeHeight;

                // Draw path line between nodes
                if (lvl < LevelCatalog.LevelsPerBiome)
                {
                    totalHeight += spacing * 0.5f;
                    BuildPathLine(totalHeight, 4f, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    totalHeight += 4f + spacing * 0.5f;
                }
            }

            totalHeight += spacing * 3;

            // Ice biome zone
            totalHeight += BuildBiomeHeader("ЛЕДЯНОЙ БИОМ", new Color(0.45f, 0.70f, 0.95f), totalHeight);

            for (int lvl = LevelCatalog.LevelsPerBiome + 1; lvl <= LevelCatalog.TotalLevels; lvl++)
            {
                totalHeight += spacing;
                BuildLevelNode(lvl, BiomeType.Ice, totalHeight, nodeHeight);
                totalHeight += nodeHeight;

                if (lvl < LevelCatalog.TotalLevels)
                {
                    totalHeight += spacing * 0.5f;
                    BuildPathLine(totalHeight, 4f, new Color(0.4f, 0.6f, 0.8f, 0.5f));
                    totalHeight += 4f + spacing * 0.5f;
                }
            }

            totalHeight += 40f;
            _contentRT.sizeDelta = new Vector2(0f, totalHeight);
        }

        private float BuildBiomeHeader(string title, Color color, float yPos)
        {
            float height = 80f;
            var go = new GameObject($"BiomeHeader_{title}");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yPos);
            rt.sizeDelta = new Vector2(0f, height);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.8f);
            bg.raycastTarget = false;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 40;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;

            return height;
        }

        private void BuildPathLine(float yPos, float height, Color color)
        {
            var go = new GameObject("PathLine");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yPos);
            rt.sizeDelta = new Vector2(6f, height);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void BuildLevelNode(int levelNum, BiomeType biome, float yPos, float height)
        {
            bool isIce = biome == BiomeType.Ice;
            Color accent = isIce
                ? new Color(0.45f, 0.70f, 0.95f)
                : new Color(0.55f, 0.80f, 0.40f);

            var go = new GameObject($"Level_{levelNum}");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 1f);
            rt.anchorMax = new Vector2(0.95f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yPos);
            rt.sizeDelta = new Vector2(0f, height);

            var btnImg = go.AddComponent<Image>();
            btnImg.color = new Color(0.15f, 0.18f, 0.22f, 0.9f);
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.30f, 0.38f);
            colors.pressedColor = new Color(0.35f, 0.40f, 0.48f);
            btn.colors = colors;

            // Level icon (circle)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0f, 0.5f);
            iconRT.anchorMax = new Vector2(0f, 0.5f);
            iconRT.pivot = new Vector2(0f, 0.5f);
            iconRT.anchoredPosition = new Vector2(20f, 0f);
            iconRT.sizeDelta = new Vector2(80f, 80f);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = accent;

            // Level number text on icon
            var numGO = new GameObject("Num");
            numGO.transform.SetParent(iconGO.transform, false);
            var numRT = numGO.AddComponent<RectTransform>();
            numRT.anchorMin = Vector2.zero;
            numRT.anchorMax = Vector2.one;
            numRT.offsetMin = Vector2.zero;
            numRT.offsetMax = Vector2.zero;
            var numTmp = numGO.AddComponent<TextMeshProUGUI>();
            numTmp.text = levelNum.ToString();
            numTmp.fontSize = 36;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;

            // Level name + info
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 1f);
            labelRT.offsetMin = new Vector2(120f, 10f);
            labelRT.offsetMax = new Vector2(-20f, -10f);
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            labelTmp.text = LevelCatalog.GetLevelName(levelNum) + "\n<size=24>10 волн | Босс на 10-й</size>";
            labelTmp.fontSize = 32;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = Color.white;

            // Lock icon overlay (hidden when unlocked)
            var lockGO = new GameObject("Lock");
            lockGO.transform.SetParent(go.transform, false);
            var lockRT = lockGO.AddComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(1f, 0.5f);
            lockRT.anchorMax = new Vector2(1f, 0.5f);
            lockRT.pivot = new Vector2(1f, 0.5f);
            lockRT.anchoredPosition = new Vector2(-20f, 0f);
            lockRT.sizeDelta = new Vector2(50f, 50f);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

            // Lock text
            var lockTxtGO = new GameObject("LockTxt");
            lockTxtGO.transform.SetParent(lockGO.transform, false);
            var lockTxtRT = lockTxtGO.AddComponent<RectTransform>();
            lockTxtRT.anchorMin = Vector2.zero;
            lockTxtRT.anchorMax = Vector2.one;
            lockTxtRT.offsetMin = Vector2.zero;
            lockTxtRT.offsetMax = Vector2.zero;
            var lockTxt = lockTxtGO.AddComponent<TextMeshProUGUI>();
            lockTxt.text = "\u0437\u0430\u043c\u043e\u043a";
            lockTxt.fontSize = 18;
            lockTxt.alignment = TextAlignmentOptions.Center;
            lockTxt.color = Color.white;

            int lvl = levelNum;
            btn.onClick.AddListener(() => OnLevelClicked(lvl));

            _nodes.Add(new LevelNode
            {
                level = levelNum,
                button = btn,
                icon = iconImg,
                label = labelTmp,
                lockIcon = lockImg,
            });
        }

        public void Show(int highestCompleted)
        {
            _highestUnlocked = highestCompleted + 1;
            RefreshNodes();
            _root.SetActive(true);

            // Scroll to current level
            if (_contentRT != null && _scroll != null)
            {
                float target = Mathf.Clamp01(1f - (float)(_highestUnlocked - 1) / LevelCatalog.TotalLevels);
                _scroll.verticalNormalizedPosition = target;
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
        }

        public bool IsOpen => _root.activeSelf;

        private void RefreshNodes()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                bool completed = node.level <= (_highestUnlocked - 1);
                bool isCurrent = node.level == _highestUnlocked;
                bool locked = node.level > _highestUnlocked;

                node.button.interactable = !locked;
                node.lockIcon.gameObject.SetActive(locked);

                if (completed)
                {
                    node.icon.color = new Color(0.3f, 0.85f, 0.4f);
                    node.label.color = new Color(0.8f, 0.9f, 0.8f);
                }
                else if (isCurrent)
                {
                    node.icon.color = new Color(1f, 0.85f, 0.2f);
                    node.label.color = Color.white;
                }
                else if (locked)
                {
                    node.icon.color = new Color(0.3f, 0.3f, 0.35f);
                    node.label.color = new Color(0.45f, 0.45f, 0.5f);
                }
            }
        }

        private void OnLevelClicked(int level)
        {
            if (level > _highestUnlocked) return;
            Hide();
            OnLevelSelected?.Invoke(level);
        }

        private void Update()
        {
            // Pulse effect on the current level node
            if (!_root.activeSelf) return;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node.level != _highestUnlocked) continue;
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 3f);
                node.icon.color = new Color(1f * pulse, 0.85f * pulse, 0.2f * pulse);
                break;
            }
        }
    }
}
