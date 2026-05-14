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
            public Image glow;
            public TextMeshProUGUI numText;
            public TextMeshProUGUI nameText;
        }

        // Zigzag x-positions for 20 levels (normalized 0..1 within safe area)
        private static readonly float[] NodeXNorm =
        {
            0.22f, 0.72f, 0.30f, 0.65f, 0.18f,
            0.75f, 0.35f, 0.68f, 0.25f, 0.50f,
            0.70f, 0.25f, 0.60f, 0.20f, 0.72f,
            0.35f, 0.65f, 0.28f, 0.58f, 0.45f,
        };

        private static readonly string[] ForestNames =
        {
            "Опушка леса", "Берёзовая роща", "Тропа охотника",
            "Старый дуб", "Лисья нора", "Грибная поляна",
            "Речная переправа", "Волчий лог", "Тёмная чаща",
            "Корень древа",
        };

        private static readonly string[] IceNames =
        {
            "Снежный перевал", "Ледяное озеро", "Метельный пик",
            "Логово йети", "Замёрзший лес", "Хрустальная пещера",
            "Ледяной мост", "Северное сияние", "Трон зимы",
            "Сердце вьюги",
        };

        private const float MapW = 1080f;
        private const float SafeMargin = 80f;
        private const float NodeSpacingY = 300f;
        private const float BiomeGap = 420f;
        private const float PadTop = 200f;
        private const float PadBot = 160f;
        private const float NodeRadius = 55f;

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
            map.Build();
            go.SetActive(false);
            return map;
        }

        private float TotalH()
        {
            return PadTop
                + LevelCatalog.LevelsPerBiome * NodeSpacingY
                + BiomeGap
                + LevelCatalog.LevelsPerBiome * NodeSpacingY
                + PadBot;
        }

        private Vector2 NodePos(int idx)
        {
            float safeW = MapW - SafeMargin * 2f;
            float x = SafeMargin + NodeXNorm[idx] * safeW - MapW * 0.5f;
            float total = TotalH();
            float y;
            if (idx < LevelCatalog.LevelsPerBiome)
                y = total - PadTop - idx * NodeSpacingY;
            else
            {
                int iceIdx = idx - LevelCatalog.LevelsPerBiome;
                y = total - PadTop - LevelCatalog.LevelsPerBiome * NodeSpacingY - BiomeGap - iceIdx * NodeSpacingY;
            }
            return new Vector2(x, y);
        }

        private void Build()
        {
            // Parchment background
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.82f, 0.74f, 0.60f);

            // Scroll view
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(_root.transform, false);
            Stretch(scrollGO);
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.005f);
            scrollGO.AddComponent<Mask>();

            var content = new GameObject("Content");
            content.transform.SetParent(scrollGO.transform, false);
            _contentRT = content.AddComponent<RectTransform>();
            _contentRT.anchorMin = new Vector2(0, 0);
            _contentRT.anchorMax = new Vector2(1, 0);
            _contentRT.pivot = new Vector2(0.5f, 0);
            _contentRT.sizeDelta = new Vector2(0, TotalH());

            _scroll = scrollGO.AddComponent<ScrollRect>();
            _scroll.content = _contentRT;
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.elasticity = 0.08f;
            _scroll.decelerationRate = 0.10f;

            // --- Layer 1: biome zone backgrounds ---
            DrawBiomeZones();

            // --- Layer 2: terrain decorations (all raycastTarget=false) ---
            DrawForestTerrain();
            DrawIceTerrain();
            DrawBiomeBorder();

            // --- Layer 3: paths between nodes ---
            DrawPaths();

            // --- Layer 4: level nodes (on top, clickable) ---
            DrawNodes();
        }

        // ───────── Biome zone fills ─────────

        private void DrawBiomeZones()
        {
            float total = TotalH();
            float forestBot = NodePos(LevelCatalog.LevelsPerBiome - 1).y - NodeSpacingY * 0.5f;

            // Forest fill
            var f = Img("ForestZone", _contentRT);
            f.rt.anchoredPosition = new Vector2(0, (total + forestBot) * 0.5f);
            f.rt.sizeDelta = new Vector2(MapW * 1.2f, total - forestBot);
            f.img.color = new Color(0.52f, 0.68f, 0.38f, 0.30f);
            f.img.raycastTarget = false;

            // Ice fill
            float iceTop = forestBot;
            var ic = Img("IceZone", _contentRT);
            ic.rt.anchoredPosition = new Vector2(0, iceTop * 0.5f);
            ic.rt.sizeDelta = new Vector2(MapW * 1.2f, iceTop);
            ic.img.color = new Color(0.72f, 0.82f, 0.94f, 0.35f);
            ic.img.raycastTarget = false;

            // Zone titles
            Label("ЗЕЛЁНЫЙ ЛЕС", 44, new Color(0.18f, 0.38f, 0.10f, 0.50f),
                new Vector2(0, total - 80f));
            Label("ЛЕДЯНОЙ КРАЙ", 44, new Color(0.15f, 0.30f, 0.55f, 0.50f),
                new Vector2(0, NodePos(LevelCatalog.LevelsPerBiome).y + NodeSpacingY * 0.7f));
        }

        // ───────── Forest decorations ─────────

        private void DrawForestTerrain()
        {
            var rng = new System.Random(42);
            float total = TotalH();
            float yTop = total - PadTop + NodeSpacingY * 0.3f;
            float yBot = NodePos(LevelCatalog.LevelsPerBiome - 1).y - NodeSpacingY * 0.4f;

            // Scattered pine trees
            for (int i = 0; i < 50; i++)
            {
                float x = Rng(rng, -MapW * 0.48f, MapW * 0.48f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, 0, LevelCatalog.LevelsPerBiome, 110f)) continue;

                float s = Rng(rng, 50f, 90f);
                DrawPineTree(x, y, s, rng);
            }

            // Bushes / grass clusters
            for (int i = 0; i < 30; i++)
            {
                float x = Rng(rng, -MapW * 0.45f, MapW * 0.45f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, 0, LevelCatalog.LevelsPerBiome, 90f)) continue;

                float s = Rng(rng, 20f, 40f);
                var bush = SprImg("Bush", _contentRT, SpriteFactory.SoftCircle());
                bush.rt.anchoredPosition = new Vector2(x, y);
                bush.rt.sizeDelta = new Vector2(s * 1.4f, s);
                float g = Rng(rng, 0.40f, 0.60f);
                bush.img.color = new Color(0.25f, g, 0.18f, 0.50f);
                bush.img.raycastTarget = false;
            }

            // Gentle hills along edges
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                float x = side * Rng(rng, MapW * 0.32f, MapW * 0.50f);
                float y = Rng(rng, yBot, yTop);
                var hill = SprImg("Hill", _contentRT, SpriteFactory.SoftCircle());
                hill.rt.anchoredPosition = new Vector2(x, y);
                hill.rt.sizeDelta = new Vector2(Rng(rng, 150f, 250f), Rng(rng, 60f, 100f));
                hill.img.color = new Color(0.48f, 0.58f, 0.35f, 0.25f);
                hill.img.raycastTarget = false;
            }

            // Small river
            float rx = MapW * 0.12f;
            var p2 = NodePos(2);
            var p6 = NodePos(6);
            for (int i = 0; i < 14; i++)
            {
                float t = i / 13f;
                float y = Mathf.Lerp(p2.y + 80f, p6.y - 50f, t);
                float x = rx + Mathf.Sin(t * Mathf.PI * 3f) * 55f;
                var seg = Img("River", _contentRT);
                seg.rt.anchoredPosition = new Vector2(x, y);
                float segH = (p2.y - p6.y + 130f) / 14f + 8f;
                seg.rt.sizeDelta = new Vector2(14f, segH);
                seg.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 3.5f) * 18f);
                seg.img.color = new Color(0.28f, 0.52f, 0.72f, 0.45f);
                seg.img.raycastTarget = false;
            }
        }

        private void DrawPineTree(float x, float y, float size, System.Random rng)
        {
            // Trunk
            var trunk = Img("Trunk", _contentRT);
            trunk.rt.anchoredPosition = new Vector2(x, y - size * 0.1f);
            trunk.rt.sizeDelta = new Vector2(size * 0.12f, size * 0.30f);
            trunk.img.color = new Color(0.42f, 0.30f, 0.18f, 0.70f);
            trunk.img.raycastTarget = false;

            // 3 layers of foliage using triangles
            for (int t = 0; t < 3; t++)
            {
                float layerY = y + size * (0.10f + t * 0.22f);
                float layerW = size * (0.55f - t * 0.12f);
                float layerH = size * 0.38f;
                var leaf = SprImg("Leaf", _contentRT, SpriteFactory.Triangle());
                leaf.rt.anchoredPosition = new Vector2(x, layerY);
                leaf.rt.sizeDelta = new Vector2(layerW, layerH);
                float green = 0.35f + t * 0.08f + (float)rng.NextDouble() * 0.12f;
                leaf.img.color = new Color(0.15f, green, 0.10f, 0.75f);
                leaf.img.raycastTarget = false;
            }
        }

        // ───────── Ice decorations ─────────

        private void DrawIceTerrain()
        {
            var rng = new System.Random(99);
            float yTop = NodePos(LevelCatalog.LevelsPerBiome).y + NodeSpacingY * 0.3f;
            float yBot = NodePos(LevelCatalog.TotalLevels - 1).y - NodeSpacingY * 0.4f;

            // Snow-capped mountains
            for (int i = 0; i < 14; i++)
            {
                float x = Rng(rng, -MapW * 0.48f, MapW * 0.48f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, LevelCatalog.LevelsPerBiome, LevelCatalog.TotalLevels, 120f))
                    continue;

                float mW = Rng(rng, 60f, 100f);
                float mH = Rng(rng, 70f, 120f);
                DrawMountain(x, y, mW, mH, rng);
            }

            // Frozen trees (blue-white pines)
            for (int i = 0; i < 30; i++)
            {
                float x = Rng(rng, -MapW * 0.46f, MapW * 0.46f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, LevelCatalog.LevelsPerBiome, LevelCatalog.TotalLevels, 100f))
                    continue;

                float s = Rng(rng, 40f, 75f);
                DrawIceTree(x, y, s, rng);
            }

            // Snowflake particles
            for (int i = 0; i < 50; i++)
            {
                float x = Rng(rng, -MapW * 0.48f, MapW * 0.48f);
                float y = Rng(rng, yBot, yTop);
                var snow = SprImg("Snow", _contentRT, SpriteFactory.Star());
                float sz = Rng(rng, 8f, 18f);
                snow.rt.anchoredPosition = new Vector2(x, y);
                snow.rt.sizeDelta = new Vector2(sz, sz);
                snow.img.color = new Color(0.88f, 0.92f, 1f, Rng(rng, 0.15f, 0.40f));
                snow.img.raycastTarget = false;
            }

            // Ice patches on ground
            for (int i = 0; i < 12; i++)
            {
                float x = Rng(rng, -MapW * 0.40f, MapW * 0.40f);
                float y = Rng(rng, yBot, yTop);
                var patch = SprImg("IcePatch", _contentRT, SpriteFactory.SoftCircle());
                patch.rt.anchoredPosition = new Vector2(x, y);
                patch.rt.sizeDelta = new Vector2(Rng(rng, 80f, 160f), Rng(rng, 30f, 60f));
                patch.img.color = new Color(0.78f, 0.88f, 0.98f, 0.20f);
                patch.img.raycastTarget = false;
            }
        }

        private void DrawIceTree(float x, float y, float size, System.Random rng)
        {
            var trunk = Img("Trunk", _contentRT);
            trunk.rt.anchoredPosition = new Vector2(x, y - size * 0.1f);
            trunk.rt.sizeDelta = new Vector2(size * 0.10f, size * 0.25f);
            trunk.img.color = new Color(0.50f, 0.48f, 0.45f, 0.55f);
            trunk.img.raycastTarget = false;

            for (int t = 0; t < 3; t++)
            {
                float layerY = y + size * (0.08f + t * 0.20f);
                float layerW = size * (0.48f - t * 0.10f);
                float layerH = size * 0.35f;
                var leaf = SprImg("IceLeaf", _contentRT, SpriteFactory.Triangle());
                leaf.rt.anchoredPosition = new Vector2(x, layerY);
                leaf.rt.sizeDelta = new Vector2(layerW, layerH);
                float blue = 0.65f + t * 0.08f;
                leaf.img.color = new Color(0.55f, 0.68f + t * 0.05f, blue, 0.65f);
                leaf.img.raycastTarget = false;
            }
        }

        private void DrawMountain(float x, float y, float w, float h, System.Random rng)
        {
            // Main peak (triangle)
            var peak = SprImg("Peak", _contentRT, SpriteFactory.Triangle());
            peak.rt.anchoredPosition = new Vector2(x, y);
            peak.rt.sizeDelta = new Vector2(w, h);
            peak.img.color = new Color(0.48f, 0.52f, 0.60f, 0.55f);
            peak.img.raycastTarget = false;

            // Snow cap on top
            var cap = SprImg("SnowCap", _contentRT, SpriteFactory.SoftCircle());
            cap.rt.anchoredPosition = new Vector2(x, y + h * 0.65f);
            cap.rt.sizeDelta = new Vector2(w * 0.40f, h * 0.25f);
            cap.img.color = new Color(0.92f, 0.95f, 1f, 0.75f);
            cap.img.raycastTarget = false;
        }

        // ───────── Biome border ─────────

        private void DrawBiomeBorder()
        {
            float forestBotY = NodePos(LevelCatalog.LevelsPerBiome - 1).y;
            float iceTopY = NodePos(LevelCatalog.LevelsPerBiome).y;
            float midY = (forestBotY + iceTopY) * 0.5f;

            // Horizontal decorative border line
            var border = Img("BiomeBorder", _contentRT);
            border.rt.anchoredPosition = new Vector2(0, midY);
            border.rt.sizeDelta = new Vector2(MapW * 0.85f, 4f);
            border.img.color = new Color(0.45f, 0.42f, 0.38f, 0.40f);
            border.img.raycastTarget = false;

            // Mountain range at border
            var rng = new System.Random(77);
            for (int i = 0; i < 7; i++)
            {
                float x = -MapW * 0.35f + i * MapW * 0.12f;
                float w = Rng(rng, 50f, 80f);
                float h = Rng(rng, 50f, 90f);
                var mtn = SprImg("BorderMtn", _contentRT, SpriteFactory.Triangle());
                mtn.rt.anchoredPosition = new Vector2(x, midY);
                mtn.rt.sizeDelta = new Vector2(w, h);
                mtn.img.color = new Color(0.52f, 0.50f, 0.48f, 0.45f);
                mtn.img.raycastTarget = false;

                var cap = SprImg("BorderCap", _contentRT, SpriteFactory.SoftCircle());
                cap.rt.anchoredPosition = new Vector2(x, midY + h * 0.55f);
                cap.rt.sizeDelta = new Vector2(w * 0.35f, h * 0.22f);
                cap.img.color = new Color(0.90f, 0.93f, 0.98f, 0.60f);
                cap.img.raycastTarget = false;
            }
        }

        // ───────── Paths ─────────

        private void DrawPaths()
        {
            for (int i = 0; i < LevelCatalog.TotalLevels - 1; i++)
            {
                var p1 = NodePos(i);
                var p2 = NodePos(i + 1);
                bool cross = (i == LevelCatalog.LevelsPerBiome - 1);

                Color c;
                if (cross)
                    c = new Color(0.55f, 0.52f, 0.48f, 0.50f);
                else if (i < LevelCatalog.LevelsPerBiome)
                    c = new Color(0.60f, 0.45f, 0.28f, 0.60f);
                else
                    c = new Color(0.62f, 0.72f, 0.82f, 0.55f);

                int segs = cross ? 10 : 6;
                float w = cross ? 8f : 10f;

                for (int s = 0; s < segs; s++)
                {
                    float t0 = s / (float)segs;
                    float t1 = (s + 1f) / segs;
                    float mx = Mathf.Lerp(p1.x, p2.x, (t0 + t1) * 0.5f);
                    float my = Mathf.Lerp(p1.y, p2.y, (t0 + t1) * 0.5f);
                    float dx = p2.x * t1 + p1.x * (1 - t1) - (p2.x * t0 + p1.x * (1 - t0));
                    float dy = p2.y * t1 + p1.y * (1 - t1) - (p2.y * t0 + p1.y * (1 - t0));
                    float len = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;

                    var seg = Img($"P{i}_{s}", _contentRT);
                    seg.rt.anchoredPosition = new Vector2(mx, my);
                    seg.rt.sizeDelta = new Vector2(w, len + 4f);
                    seg.rt.localRotation = Quaternion.Euler(0, 0, -angle);
                    seg.img.color = c;
                    seg.img.raycastTarget = false;
                }
            }
        }

        // ───────── Level nodes ─────────

        private void DrawNodes()
        {
            for (int i = 0; i < LevelCatalog.TotalLevels; i++)
            {
                int lvl = i + 1;
                var pos = NodePos(i);
                bool ice = lvl > LevelCatalog.LevelsPerBiome;
                string locName = ice
                    ? IceNames[i - LevelCatalog.LevelsPerBiome]
                    : ForestNames[i];

                DrawOneNode(lvl, pos, locName, ice);
            }
        }

        private void DrawOneNode(int lvl, Vector2 pos, string locName, bool ice)
        {
            Color accent = ice
                ? new Color(0.40f, 0.65f, 0.92f)
                : new Color(0.45f, 0.75f, 0.30f);

            // Glow behind node
            var glow = SprImg($"Glow_{lvl}", _contentRT, SpriteFactory.SoftCircle());
            glow.rt.anchoredPosition = pos;
            glow.rt.sizeDelta = new Vector2(NodeRadius * 3.2f, NodeRadius * 3.2f);
            glow.img.color = new Color(accent.r, accent.g, accent.b, 0.25f);
            glow.img.raycastTarget = false;

            // Dark circle outline
            var outline = SprImg($"Outline_{lvl}", _contentRT, SpriteFactory.Circle());
            outline.rt.anchoredPosition = pos;
            outline.rt.sizeDelta = new Vector2(NodeRadius * 2f + 12f, NodeRadius * 2f + 12f);
            outline.img.color = new Color(0.20f, 0.18f, 0.15f, 0.80f);
            outline.img.raycastTarget = false;

            // Main circle button
            var nodeGO = new GameObject($"Level_{lvl}");
            nodeGO.transform.SetParent(_contentRT, false);
            var nodeRT = nodeGO.AddComponent<RectTransform>();
            nodeRT.anchorMin = new Vector2(0.5f, 0);
            nodeRT.anchorMax = new Vector2(0.5f, 0);
            nodeRT.pivot = new Vector2(0.5f, 0.5f);
            nodeRT.anchoredPosition = pos;
            nodeRT.sizeDelta = new Vector2(NodeRadius * 2f, NodeRadius * 2f);

            var nodeImg = nodeGO.AddComponent<Image>();
            nodeImg.sprite = SpriteFactory.Circle();
            nodeImg.color = accent;

            var btn = nodeGO.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cols.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            cols.disabledColor = new Color(0.50f, 0.50f, 0.50f);
            btn.colors = cols;

            int captured = lvl;
            btn.onClick.AddListener(() => OnLevelClicked(captured));

            // Number text
            var numGO = new GameObject("Num");
            numGO.transform.SetParent(nodeGO.transform, false);
            var numRT = numGO.AddComponent<RectTransform>();
            numRT.anchorMin = Vector2.zero;
            numRT.anchorMax = Vector2.one;
            numRT.offsetMin = Vector2.zero;
            numRT.offsetMax = Vector2.zero;
            var numTmp = numGO.AddComponent<TextMeshProUGUI>();
            numTmp.text = lvl.ToString();
            numTmp.fontSize = 40;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.raycastTarget = false;

            // Location name below
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(_contentRT, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.5f, 0);
            nameRT.anchorMax = new Vector2(0.5f, 0);
            nameRT.pivot = new Vector2(0.5f, 1f);
            nameRT.anchoredPosition = new Vector2(pos.x, pos.y - NodeRadius - 8f);
            nameRT.sizeDelta = new Vector2(280f, 36f);
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = locName;
            nameTmp.fontSize = 24;
            nameTmp.fontStyle = FontStyles.Italic;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = new Color(0.28f, 0.22f, 0.15f, 0.80f);
            nameTmp.raycastTarget = false;

            _nodes.Add(new LevelNode
            {
                level = lvl,
                button = btn,
                icon = nodeImg,
                glow = glow.img,
                numText = numTmp,
                nameText = nameTmp,
            });
        }

        // ───────── Public API ─────────

        public void Show(int highestCompleted)
        {
            _highestUnlocked = highestCompleted + 1;
            Refresh();
            _root.SetActive(true);

            if (_scroll != null)
            {
                float t = Mathf.Clamp01(1f - (float)(_highestUnlocked - 1) / LevelCatalog.TotalLevels);
                _scroll.verticalNormalizedPosition = t;
            }
        }

        public void Hide() => _root.SetActive(false);
        public bool IsOpen => _root.activeSelf;

        private void Refresh()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                bool done = n.level < _highestUnlocked;
                bool current = n.level == _highestUnlocked;
                bool locked = n.level > _highestUnlocked;

                n.button.interactable = !locked;

                if (done)
                {
                    n.icon.color = new Color(0.30f, 0.80f, 0.35f);
                    n.glow.color = new Color(0.30f, 0.80f, 0.35f, 0.20f);
                    n.numText.text = "\u2714";
                    n.numText.fontSize = 44;
                    n.numText.color = Color.white;
                    n.nameText.color = new Color(0.22f, 0.42f, 0.22f, 0.65f);
                }
                else if (current)
                {
                    n.icon.color = new Color(1f, 0.82f, 0.18f);
                    n.glow.color = new Color(1f, 0.82f, 0.18f, 0.35f);
                    n.numText.text = n.level.ToString();
                    n.numText.fontSize = 40;
                    n.numText.color = Color.white;
                    n.nameText.color = new Color(0.28f, 0.22f, 0.15f, 0.90f);
                }
                else
                {
                    n.icon.color = new Color(0.42f, 0.40f, 0.38f);
                    n.glow.color = new Color(0.42f, 0.40f, 0.38f, 0.10f);
                    n.numText.text = n.level.ToString();
                    n.numText.fontSize = 40;
                    n.numText.color = new Color(0.65f, 0.62f, 0.58f);
                    n.nameText.color = new Color(0.40f, 0.38f, 0.35f, 0.40f);
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
                var n = _nodes[i];
                if (n.level != _highestUnlocked) continue;

                float t = Time.unscaledTime;
                float pulse = 0.78f + 0.22f * Mathf.Sin(t * 3.2f);
                n.icon.color = new Color(pulse, 0.82f * pulse, 0.18f * pulse);

                float gp = 0.25f + 0.15f * Mathf.Sin(t * 2.5f);
                n.glow.color = new Color(1f, 0.82f, 0.18f, gp);

                float sc = 1f + 0.05f * Mathf.Sin(t * 3.2f);
                n.button.transform.localScale = Vector3.one * sc;
                break;
            }
        }

        // ───────── Helpers ─────────

        private struct R { public RectTransform rt; public Image img; }

        private R Img(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return new R { rt = rt, img = img };
        }

        private R SprImg(string name, RectTransform parent, Sprite spr)
        {
            var r = Img(name, parent);
            r.img.sprite = spr;
            return r;
        }

        private void Label(string text, int size, Color c, Vector2 pos)
        {
            var go = new GameObject("ZoneLabel");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(600f, 60f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = c;
            tmp.raycastTarget = false;
        }

        private static void Stretch(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private bool TooCloseToNode(float x, float y, int from, int to, float dist)
        {
            for (int n = from; n < to; n++)
            {
                var p = NodePos(n);
                if (Mathf.Abs(x - p.x) < dist && Mathf.Abs(y - p.y) < dist * 0.8f)
                    return true;
            }
            return false;
        }

        private static float Rng(System.Random r, float min, float max)
        {
            return min + (float)r.NextDouble() * (max - min);
        }
    }
}
