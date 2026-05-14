using System;
using System.Collections.Generic;
using StickEvolve.Core;
using StickEvolve.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.Levels
{
    public class LevelMapUI : MonoBehaviour
    {
        public event Action<int> OnLevelSelected;

        private Canvas _canvas;
        private GameObject _root;
        private ScrollRect _scroll;
        private RectTransform _contentRT;
        private readonly List<LevelNode> _nodes = new();

        private int _highestUnlocked;

        private struct LevelNode
        {
            public int level;
            public Button button;
            public Image icon;
            public Image ring;
            public TextMeshProUGUI numText;
            public TextMeshProUGUI nameText;
        }

        // Winding path positions for 20 levels (x = 0..1 normalized, y = cumulative height)
        private static readonly float[] PathX =
        {
            0.25f, 0.60f, 0.35f, 0.72f, 0.20f,
            0.55f, 0.40f, 0.70f, 0.30f, 0.50f,
            0.65f, 0.28f, 0.58f, 0.22f, 0.70f,
            0.38f, 0.62f, 0.30f, 0.55f, 0.45f,
        };

        private static readonly string[] ForestNames =
        {
            "Опушка леса", "Берёзовая роща", "Тропа охотника", "Старый дуб",
            "Лисья нора", "Грибная поляна", "Речная переправа", "Волчий лог",
            "Тёмная чаща", "Корень древа",
        };

        private static readonly string[] IceNames =
        {
            "Снежный перевал", "Ледяное озеро", "Метельный пик", "Логово йети",
            "Замёрзший лес", "Хрустальная пещера", "Ледяной мост", "Северное сияние",
            "Трон зимы", "Сердце вьюги",
        };

        // Map layout constants
        private const float MapWidth = 1080f;
        private const float NodeSpacingY = 260f;
        private const float BiomeGap = 350f;
        private const float TopPadding = 180f;
        private const float BottomPadding = 120f;
        private const float NodeSize = 90f;

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
            // Full-screen container
            var bgImg = _root.AddComponent<Image>();
            bgImg.color = new Color(0.76f, 0.68f, 0.55f); // parchment base

            // Scroll setup
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(_root.transform, false);
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;
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
            _scroll.elasticity = 0.1f;
            _scroll.decelerationRate = 0.12f;

            float totalHeight = TopPadding
                + LevelCatalog.LevelsPerBiome * NodeSpacingY
                + BiomeGap
                + LevelCatalog.LevelsPerBiome * NodeSpacingY
                + BottomPadding;
            _contentRT.sizeDelta = new Vector2(0f, totalHeight);

            // Draw biome backgrounds
            BuildBiomeBackgrounds(totalHeight);

            // Draw decorations
            BuildForestDecorations(totalHeight);
            BuildIceDecorations(totalHeight);

            // Draw path lines
            BuildPaths(totalHeight);

            // Draw level nodes
            BuildAllNodes(totalHeight);
        }

        private float GetNodeY(int index, float totalHeight)
        {
            if (index < LevelCatalog.LevelsPerBiome)
                return totalHeight - TopPadding - index * NodeSpacingY;
            int iceIdx = index - LevelCatalog.LevelsPerBiome;
            return totalHeight - TopPadding - LevelCatalog.LevelsPerBiome * NodeSpacingY - BiomeGap - iceIdx * NodeSpacingY;
        }

        private float GetNodeX(int index)
        {
            return PathX[index] * MapWidth - MapWidth * 0.5f;
        }

        private void BuildBiomeBackgrounds(float totalHeight)
        {
            // Forest zone background (bottom half — levels 1-10)
            float forestTop = totalHeight;
            float forestBot = totalHeight - TopPadding - LevelCatalog.LevelsPerBiome * NodeSpacingY - BiomeGap * 0.4f;
            var forest = MakeRect("ForestBG", _contentRT);
            forest.rt.anchoredPosition = new Vector2(0f, forestBot);
            forest.rt.sizeDelta = new Vector2(MapWidth + 200f, forestTop - forestBot);
            forest.img.color = new Color(0.58f, 0.72f, 0.42f, 0.45f); // green tint

            // Biome transition zone
            float transBot = forestBot - BiomeGap * 0.2f;
            var trans = MakeRect("TransBG", _contentRT);
            trans.rt.anchoredPosition = new Vector2(0f, transBot);
            trans.rt.sizeDelta = new Vector2(MapWidth + 200f, forestBot - transBot + 10f);
            trans.img.color = new Color(0.55f, 0.62f, 0.58f, 0.35f);

            // Ice zone background (top of scroll = upper area, levels 11-20)
            float iceTop = transBot + 10f;
            var ice = MakeRect("IceBG", _contentRT);
            ice.rt.anchoredPosition = new Vector2(0f, 0f);
            ice.rt.sizeDelta = new Vector2(MapWidth + 200f, iceTop);
            ice.img.color = new Color(0.72f, 0.82f, 0.92f, 0.50f); // blue tint

            // Forest zone label
            MakeLabel("ЗЕЛЁНЫЙ ЛЕС", 40, new Color(0.20f, 0.40f, 0.12f, 0.55f),
                new Vector2(0f, forestTop - 60f), _contentRT);

            // Ice zone label
            float iceLabelY = GetNodeY(LevelCatalog.LevelsPerBiome, totalHeight) + NodeSpacingY * 0.6f;
            MakeLabel("ЛЕДЯНОЙ КРАЙ", 40, new Color(0.20f, 0.35f, 0.60f, 0.55f),
                new Vector2(0f, iceLabelY), _contentRT);
        }

        private void BuildForestDecorations(float totalHeight)
        {
            var rng = new System.Random(42);

            // Trees scattered across the forest zone
            for (int i = 0; i < 55; i++)
            {
                float x = (float)(rng.NextDouble() * MapWidth - MapWidth * 0.5f);
                float yBase = GetNodeY(0, totalHeight);
                float yEnd = GetNodeY(LevelCatalog.LevelsPerBiome - 1, totalHeight);
                float y = yEnd + (float)rng.NextDouble() * (yBase - yEnd + NodeSpacingY);

                bool tooClose = false;
                for (int n = 0; n < LevelCatalog.LevelsPerBiome; n++)
                {
                    float nx = GetNodeX(n);
                    float ny = GetNodeY(n, totalHeight);
                    if (Mathf.Abs(x - nx) < 100f && Mathf.Abs(y - ny) < 80f)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                float size = 30f + (float)rng.NextDouble() * 35f;
                float shade = 0.25f + (float)rng.NextDouble() * 0.25f;
                Color treeColor = new Color(shade, 0.45f + (float)rng.NextDouble() * 0.25f, shade * 0.5f, 0.75f);
                BuildTreeDecor(x, y, size, treeColor);
            }

            // Hills / small mountains along edges
            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (MapWidth * 0.38f + (float)rng.NextDouble() * MapWidth * 0.12f);
                float yBase = GetNodeY(0, totalHeight);
                float yEnd = GetNodeY(LevelCatalog.LevelsPerBiome - 1, totalHeight);
                float y = yEnd + (float)rng.NextDouble() * (yBase - yEnd);
                float w = 100f + (float)rng.NextDouble() * 80f;
                float h = 50f + (float)rng.NextDouble() * 40f;
                BuildHillDecor(x, y, w, h, new Color(0.45f, 0.55f, 0.35f, 0.40f));
            }

            // Small river winding through forest
            float riverX = MapWidth * 0.15f;
            float riverYTop = GetNodeY(2, totalHeight) + 60f;
            float riverYBot = GetNodeY(6, totalHeight) - 40f;
            for (int i = 0; i < 12; i++)
            {
                float t = i / 11f;
                float y = Mathf.Lerp(riverYTop, riverYBot, t);
                float x = riverX + Mathf.Sin(t * Mathf.PI * 2.5f) * 45f;
                var seg = MakeRect($"River_{i}", _contentRT);
                seg.rt.anchoredPosition = new Vector2(x, y);
                seg.rt.sizeDelta = new Vector2(18f, NodeSpacingY * 0.15f);
                seg.rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 15f);
                seg.img.color = new Color(0.30f, 0.55f, 0.75f, 0.50f);
                seg.img.raycastTarget = false;
            }
        }

        private void BuildIceDecorations(float totalHeight)
        {
            var rng = new System.Random(99);

            // Snow-capped mountains
            for (int i = 0; i < 12; i++)
            {
                float x = (float)(rng.NextDouble() * MapWidth - MapWidth * 0.5f);
                float yBase = GetNodeY(LevelCatalog.LevelsPerBiome, totalHeight);
                float yEnd = GetNodeY(LevelCatalog.TotalLevels - 1, totalHeight);
                float y = yEnd + (float)rng.NextDouble() * (yBase - yEnd + NodeSpacingY);

                bool tooClose = false;
                for (int n = LevelCatalog.LevelsPerBiome; n < LevelCatalog.TotalLevels; n++)
                {
                    float nx = GetNodeX(n);
                    float ny = GetNodeY(n, totalHeight);
                    if (Mathf.Abs(x - nx) < 110f && Mathf.Abs(y - ny) < 90f)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                float mw = 60f + (float)rng.NextDouble() * 70f;
                float mh = 50f + (float)rng.NextDouble() * 60f;
                BuildMountainDecor(x, y, mw, mh);
            }

            // Snowflake dots / ice crystals
            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rng.NextDouble() * MapWidth - MapWidth * 0.5f);
                float yBase = GetNodeY(LevelCatalog.LevelsPerBiome, totalHeight);
                float yEnd = GetNodeY(LevelCatalog.TotalLevels - 1, totalHeight);
                float y = yEnd + (float)rng.NextDouble() * (yBase - yEnd + NodeSpacingY);

                var snow = MakeImageRect($"Snow_{i}", _contentRT, SpriteFactory.SoftCircle());
                snow.rt.anchoredPosition = new Vector2(x, y);
                float sz = 8f + (float)rng.NextDouble() * 14f;
                snow.rt.sizeDelta = new Vector2(sz, sz);
                snow.img.color = new Color(0.90f, 0.94f, 1f, 0.25f + (float)rng.NextDouble() * 0.20f);
                snow.img.raycastTarget = false;
            }

            // Frozen trees (white/blue tint)
            for (int i = 0; i < 25; i++)
            {
                float x = (float)(rng.NextDouble() * MapWidth - MapWidth * 0.5f);
                float yBase = GetNodeY(LevelCatalog.LevelsPerBiome, totalHeight);
                float yEnd = GetNodeY(LevelCatalog.TotalLevels - 1, totalHeight);
                float y = yEnd + (float)rng.NextDouble() * (yBase - yEnd + NodeSpacingY);

                bool tooClose = false;
                for (int n = LevelCatalog.LevelsPerBiome; n < LevelCatalog.TotalLevels; n++)
                {
                    float nx = GetNodeX(n);
                    float ny = GetNodeY(n, totalHeight);
                    if (Mathf.Abs(x - nx) < 100f && Mathf.Abs(y - ny) < 80f)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                float size = 25f + (float)rng.NextDouble() * 30f;
                Color iceTreeColor = new Color(
                    0.60f + (float)rng.NextDouble() * 0.15f,
                    0.72f + (float)rng.NextDouble() * 0.15f,
                    0.85f + (float)rng.NextDouble() * 0.10f,
                    0.65f);
                BuildTreeDecor(x, y, size, iceTreeColor);
            }
        }

        private void BuildTreeDecor(float x, float y, float size, Color color)
        {
            // Trunk
            var trunk = MakeRect($"Trunk", _contentRT);
            trunk.rt.anchoredPosition = new Vector2(x, y - size * 0.15f);
            trunk.rt.sizeDelta = new Vector2(size * 0.15f, size * 0.35f);
            trunk.img.color = new Color(0.40f, 0.28f, 0.18f, color.a * 0.8f);
            trunk.img.raycastTarget = false;

            // Crown (triangle-ish using stacked rects)
            for (int t = 0; t < 3; t++)
            {
                float tier = t / 2f;
                float w = size * (1f - tier * 0.30f);
                float h = size * 0.45f;
                float ty = y + t * size * 0.25f;
                var leaf = MakeRect($"Leaf_{t}", _contentRT);
                leaf.rt.anchoredPosition = new Vector2(x, ty);
                leaf.rt.sizeDelta = new Vector2(w, h);
                float darken = 1f - t * 0.08f;
                leaf.img.color = new Color(color.r * darken, color.g * darken, color.b * darken, color.a);
                leaf.img.raycastTarget = false;
            }
        }

        private void BuildHillDecor(float x, float y, float w, float h, Color color)
        {
            var hill = MakeImageRect($"Hill", _contentRT, SpriteFactory.SoftCircle());
            hill.rt.anchoredPosition = new Vector2(x, y);
            hill.rt.sizeDelta = new Vector2(w, h);
            hill.img.color = color;
            hill.img.raycastTarget = false;
        }

        private void BuildMountainDecor(float x, float y, float w, float h)
        {
            // Mountain body
            var body = MakeRect($"Mtn", _contentRT);
            body.rt.anchoredPosition = new Vector2(x, y);
            body.rt.sizeDelta = new Vector2(w * 0.15f, h);
            body.rt.localRotation = Quaternion.identity;
            body.img.color = new Color(0.55f, 0.58f, 0.65f, 0.55f);
            body.img.raycastTarget = false;

            // Left slope
            var sl = MakeRect($"MtnL", _contentRT);
            sl.rt.anchoredPosition = new Vector2(x - w * 0.22f, y);
            sl.rt.sizeDelta = new Vector2(w * 0.55f, h * 0.85f);
            sl.rt.localRotation = Quaternion.Euler(0f, 0f, 12f);
            sl.img.color = new Color(0.50f, 0.55f, 0.63f, 0.40f);
            sl.img.raycastTarget = false;

            // Right slope
            var sr = MakeRect($"MtnR", _contentRT);
            sr.rt.anchoredPosition = new Vector2(x + w * 0.22f, y);
            sr.rt.sizeDelta = new Vector2(w * 0.55f, h * 0.85f);
            sr.rt.localRotation = Quaternion.Euler(0f, 0f, -12f);
            sr.img.color = new Color(0.50f, 0.55f, 0.63f, 0.40f);
            sr.img.raycastTarget = false;

            // Snow cap
            var cap = MakeImageRect($"MtnCap", _contentRT, SpriteFactory.SoftCircle());
            cap.rt.anchoredPosition = new Vector2(x, y + h * 0.6f);
            cap.rt.sizeDelta = new Vector2(w * 0.35f, h * 0.35f);
            cap.img.color = new Color(0.92f, 0.95f, 1f, 0.70f);
            cap.img.raycastTarget = false;
        }

        private void BuildPaths(float totalHeight)
        {
            for (int i = 0; i < LevelCatalog.TotalLevels - 1; i++)
            {
                // Skip path between biomes boundary (draw special transition)
                float x1 = GetNodeX(i);
                float y1 = GetNodeY(i, totalHeight);
                float x2 = GetNodeX(i + 1);
                float y2 = GetNodeY(i + 1, totalHeight);

                bool isBiomeBorder = (i == LevelCatalog.LevelsPerBiome - 1);
                Color pathColor = isBiomeBorder
                    ? new Color(0.50f, 0.55f, 0.60f, 0.45f)
                    : (i < LevelCatalog.LevelsPerBiome
                        ? new Color(0.55f, 0.42f, 0.25f, 0.55f)
                        : new Color(0.60f, 0.70f, 0.80f, 0.50f));

                int segments = isBiomeBorder ? 8 : 5;
                for (int s = 0; s < segments; s++)
                {
                    float t0 = s / (float)segments;
                    float t1 = (s + 1f) / segments;
                    float sx = Mathf.Lerp(x1, x2, (t0 + t1) * 0.5f);
                    float sy = Mathf.Lerp(y1, y2, (t0 + t1) * 0.5f);
                    float dx = Mathf.Lerp(x1, x2, t1) - Mathf.Lerp(x1, x2, t0);
                    float dy = Mathf.Lerp(y1, y2, t1) - Mathf.Lerp(y1, y2, t0);
                    float len = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;

                    var seg = MakeRect($"Path_{i}_{s}", _contentRT);
                    seg.rt.anchoredPosition = new Vector2(sx, sy);
                    seg.rt.sizeDelta = new Vector2(isBiomeBorder ? 6f : 8f, len + 2f);
                    seg.rt.localRotation = Quaternion.Euler(0f, 0f, -angle);
                    seg.img.color = pathColor;
                    seg.img.raycastTarget = false;
                }

                // Dashed decoration along path
                if (!isBiomeBorder)
                {
                    for (int d = 1; d < 3; d++)
                    {
                        float dt = d / 3f;
                        float dotX = Mathf.Lerp(x1, x2, dt);
                        float dotY = Mathf.Lerp(y1, y2, dt);
                        var dot = MakeImageRect($"Dot_{i}_{d}", _contentRT, SpriteFactory.Circle());
                        dot.rt.anchoredPosition = new Vector2(dotX, dotY);
                        dot.rt.sizeDelta = new Vector2(10f, 10f);
                        dot.img.color = new Color(pathColor.r, pathColor.g, pathColor.b, pathColor.a * 0.6f);
                        dot.img.raycastTarget = false;
                    }
                }
            }
        }

        private void BuildAllNodes(float totalHeight)
        {
            for (int i = 0; i < LevelCatalog.TotalLevels; i++)
            {
                int levelNum = i + 1;
                float x = GetNodeX(i);
                float y = GetNodeY(i, totalHeight);
                bool isIce = levelNum > LevelCatalog.LevelsPerBiome;
                string name = isIce ? IceNames[i - LevelCatalog.LevelsPerBiome] : ForestNames[i];
                BuildLevelNode(levelNum, x, y, name, isIce);
            }
        }

        private void BuildLevelNode(int levelNum, float x, float y, string locationName, bool isIce)
        {
            Color accent = isIce
                ? new Color(0.45f, 0.70f, 0.95f)
                : new Color(0.55f, 0.80f, 0.40f);

            // Outer ring (glow)
            var ringGO = new GameObject($"Ring_{levelNum}");
            ringGO.transform.SetParent(_contentRT, false);
            var ringRT = ringGO.AddComponent<RectTransform>();
            ringRT.anchoredPosition = new Vector2(x, y);
            ringRT.sizeDelta = new Vector2(NodeSize + 20f, NodeSize + 20f);
            var ringImg = ringGO.AddComponent<Image>();
            ringImg.sprite = SpriteFactory.SoftCircle();
            ringImg.color = new Color(accent.r, accent.g, accent.b, 0.4f);
            ringImg.raycastTarget = false;

            // Node circle (button)
            var go = new GameObject($"Level_{levelNum}");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(NodeSize, NodeSize);

            var btnImg = go.AddComponent<Image>();
            btnImg.sprite = SpriteFactory.Circle();
            btnImg.color = accent;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(accent.r * 1.2f, accent.g * 1.2f, accent.b * 1.2f);
            colors.pressedColor = new Color(accent.r * 0.8f, accent.g * 0.8f, accent.b * 0.8f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.38f);
            btn.colors = colors;

            // Level number
            var numGO = new GameObject("Num");
            numGO.transform.SetParent(go.transform, false);
            var numRT = numGO.AddComponent<RectTransform>();
            numRT.anchorMin = Vector2.zero;
            numRT.anchorMax = Vector2.one;
            numRT.offsetMin = Vector2.zero;
            numRT.offsetMax = Vector2.zero;
            var numTmp = numGO.AddComponent<TextMeshProUGUI>();
            numTmp.text = levelNum.ToString();
            numTmp.fontSize = 36;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;

            // Location name label (below the node)
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(_contentRT, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchoredPosition = new Vector2(x, y - NodeSize * 0.65f);
            nameRT.sizeDelta = new Vector2(250f, 40f);
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = locationName;
            nameTmp.fontSize = 22;
            nameTmp.fontStyle = FontStyles.Italic;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = new Color(0.25f, 0.20f, 0.15f, 0.80f);

            int lvl = levelNum;
            btn.onClick.AddListener(() => OnLevelClicked(lvl));

            _nodes.Add(new LevelNode
            {
                level = levelNum,
                button = btn,
                icon = btnImg,
                ring = ringImg,
                numText = numTmp,
                nameText = nameTmp,
            });
        }

        public void Show(int highestCompleted)
        {
            _highestUnlocked = highestCompleted + 1;
            RefreshNodes();
            _root.SetActive(true);

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

                if (completed)
                {
                    node.icon.color = new Color(0.30f, 0.85f, 0.40f);
                    node.ring.color = new Color(0.30f, 0.85f, 0.40f, 0.30f);
                    node.numText.text = "\u2714";
                    node.numText.fontSize = 40;
                    node.nameText.color = new Color(0.20f, 0.45f, 0.20f, 0.70f);
                }
                else if (isCurrent)
                {
                    node.icon.color = new Color(1f, 0.85f, 0.20f);
                    node.ring.color = new Color(1f, 0.85f, 0.20f, 0.50f);
                    node.numText.text = node.level.ToString();
                    node.numText.fontSize = 36;
                    node.nameText.color = new Color(0.25f, 0.20f, 0.15f, 0.90f);
                }
                else
                {
                    node.icon.color = new Color(0.40f, 0.40f, 0.42f);
                    node.ring.color = new Color(0.40f, 0.40f, 0.42f, 0.20f);
                    node.numText.text = node.level.ToString();
                    node.numText.fontSize = 36;
                    node.numText.color = new Color(0.7f, 0.7f, 0.7f);
                    node.nameText.color = new Color(0.40f, 0.38f, 0.35f, 0.45f);
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
            if (!_root.activeSelf) return;
            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];
                if (node.level != _highestUnlocked) continue;
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
                node.icon.color = new Color(1f * pulse, 0.85f * pulse, 0.20f * pulse);
                float ringPulse = 0.35f + 0.20f * Mathf.Sin(Time.unscaledTime * 2f);
                node.ring.color = new Color(1f, 0.85f, 0.20f, ringPulse);
                float scale = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 3f);
                node.button.transform.localScale = Vector3.one * scale;
                break;
            }
        }

        // --- Helpers ---

        private struct RectResult { public RectTransform rt; public Image img; }

        private RectResult MakeRect(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return new RectResult { rt = rt, img = img };
        }

        private RectResult MakeImageRect(string name, RectTransform parent, Sprite sprite)
        {
            var r = MakeRect(name, parent);
            r.img.sprite = sprite;
            return r;
        }

        private void MakeLabel(string text, int fontSize, Color color, Vector2 pos, RectTransform parent)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600f, 60f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
        }
    }
}
