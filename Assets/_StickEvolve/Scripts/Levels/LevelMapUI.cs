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
            public Image rim;
            public Image starA;
            public Image starB;
            public Image starC;
            public TextMeshProUGUI numText;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI lockText;
        }

        private static readonly float[] NodeXNorm =
        {
            0.20f, 0.68f, 0.35f, 0.73f, 0.26f,
            0.62f, 0.30f, 0.76f, 0.43f, 0.58f,
            0.72f, 0.28f, 0.63f, 0.22f, 0.70f,
            0.34f, 0.74f, 0.31f, 0.60f, 0.45f,
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
        private const float SafeMargin = 96f;
        private const float NodeSpacingY = 330f;
        private const float BiomeGap = 500f;
        private const float PadTop = 260f;
        private const float PadBot = 240f;
        private const float NodeRadius = 62f;

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
            var bg = _root.AddComponent<Image>();
            bg.color = new Color(0.36f, 0.25f, 0.15f);

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

            DrawParchmentBackdrop();
            DrawBiomeZones();

            DrawForestTerrain();
            DrawIceTerrain();
            DrawBiomeBorder();

            DrawPaths();

            DrawNodes();
            DrawStartCallout();
        }

        private void DrawParchmentBackdrop()
        {
            float total = TotalH();

            var paper = Img("ParchmentSheet", _contentRT);
            paper.rt.anchoredPosition = new Vector2(0, total * 0.5f);
            paper.rt.sizeDelta = new Vector2(MapW * 1.24f, total + 80f);
            paper.img.color = new Color(0.70f, 0.58f, 0.38f);
            paper.img.raycastTarget = false;

            var inner = Img("ParchmentInner", _contentRT);
            inner.rt.anchoredPosition = new Vector2(0, total * 0.5f);
            inner.rt.sizeDelta = new Vector2(MapW * 1.02f, total - 70f);
            inner.img.color = new Color(0.83f, 0.72f, 0.50f, 0.72f);
            inner.img.raycastTarget = false;

            DrawFrameLine("FrameL", new Vector2(-MapW * 0.51f, total * 0.5f), new Vector2(12f, total - 100f), 0f);
            DrawFrameLine("FrameR", new Vector2(MapW * 0.51f, total * 0.5f), new Vector2(12f, total - 100f), 0f);
            DrawFrameLine("FrameT", new Vector2(0, total - 70f), new Vector2(MapW * 1.02f, 12f), 0f);
            DrawFrameLine("FrameB", new Vector2(0, 70f), new Vector2(MapW * 1.02f, 12f), 0f);

            var rng = new System.Random(7);
            for (int i = 0; i < 55; i++)
            {
                var stain = SprImg("PaperStain", _contentRT, SpriteFactory.SoftCircle());
                stain.rt.anchoredPosition = new Vector2(Rng(rng, -MapW * 0.48f, MapW * 0.48f), Rng(rng, 100f, total - 100f));
                float s = Rng(rng, 30f, 125f);
                stain.rt.sizeDelta = new Vector2(s * Rng(rng, 0.9f, 1.8f), s * Rng(rng, 0.45f, 0.9f));
                stain.img.color = new Color(0.42f, 0.25f, 0.10f, Rng(rng, 0.035f, 0.085f));
                stain.img.raycastTarget = false;
            }

            for (int i = 0; i < 38; i++)
            {
                var fleck = SprImg("InkFleck", _contentRT, SpriteFactory.Circle());
                fleck.rt.anchoredPosition = new Vector2(Rng(rng, -MapW * 0.50f, MapW * 0.50f), Rng(rng, 85f, total - 85f));
                float s = Rng(rng, 3f, 8f);
                fleck.rt.sizeDelta = new Vector2(s, s);
                fleck.img.color = new Color(0.22f, 0.15f, 0.08f, Rng(rng, 0.08f, 0.17f));
                fleck.img.raycastTarget = false;
            }
        }

        private void DrawFrameLine(string name, Vector2 pos, Vector2 size, float rot)
        {
            var line = Img(name, _contentRT);
            line.rt.anchoredPosition = pos;
            line.rt.sizeDelta = size;
            line.rt.localRotation = Quaternion.Euler(0, 0, rot);
            line.img.color = new Color(0.33f, 0.20f, 0.10f, 0.48f);
            line.img.raycastTarget = false;
        }

        private void DrawBiomeZones()
        {
            float total = TotalH();
            float forestBot = NodePos(LevelCatalog.LevelsPerBiome - 1).y - NodeSpacingY * 0.5f;

            var f = Img("ForestZone", _contentRT);
            f.rt.anchoredPosition = new Vector2(0, (total + forestBot) * 0.5f);
            f.rt.sizeDelta = new Vector2(MapW * 1.2f, total - forestBot);
            f.img.color = new Color(0.42f, 0.65f, 0.28f, 0.42f);
            f.img.raycastTarget = false;

            float iceTop = forestBot;
            var ic = Img("IceZone", _contentRT);
            ic.rt.anchoredPosition = new Vector2(0, iceTop * 0.5f);
            ic.rt.sizeDelta = new Vector2(MapW * 1.2f, iceTop);
            ic.img.color = new Color(0.62f, 0.78f, 0.96f, 0.43f);
            ic.img.raycastTarget = false;

            var forestShade = SprImg("ForestCanopyShade", _contentRT, SpriteFactory.SoftCircle());
            forestShade.rt.anchoredPosition = new Vector2(-MapW * 0.24f, total - 520f);
            forestShade.rt.sizeDelta = new Vector2(900f, 680f);
            forestShade.img.color = new Color(0.10f, 0.35f, 0.12f, 0.16f);
            forestShade.img.raycastTarget = false;

            var iceShade = SprImg("IceMistShade", _contentRT, SpriteFactory.SoftCircle());
            iceShade.rt.anchoredPosition = new Vector2(MapW * 0.20f, iceTop * 0.44f);
            iceShade.rt.sizeDelta = new Vector2(880f, 760f);
            iceShade.img.color = new Color(0.68f, 0.90f, 1f, 0.22f);
            iceShade.img.raycastTarget = false;

            BannerLabel("ЗЕЛЁНЫЙ ЛЕС", new Vector2(0, total - 105f), new Color(0.18f, 0.42f, 0.12f), new Color(0.74f, 0.86f, 0.44f, 0.78f));
            BannerLabel("ЛЕДЯНОЙ КРАЙ", new Vector2(0, NodePos(LevelCatalog.LevelsPerBiome).y + NodeSpacingY * 0.72f), new Color(0.15f, 0.34f, 0.62f), new Color(0.72f, 0.88f, 1f, 0.80f));
        }

        private void DrawForestTerrain()
        {
            var rng = new System.Random(42);
            float total = TotalH();
            float yTop = total - PadTop + NodeSpacingY * 0.3f;
            float yBot = NodePos(LevelCatalog.LevelsPerBiome - 1).y - NodeSpacingY * 0.4f;

            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1 : 1;
                var canopy = SprImg("ForestCanopy", _contentRT, SpriteFactory.SoftCircle());
                canopy.rt.anchoredPosition = new Vector2(side * Rng(rng, MapW * 0.30f, MapW * 0.53f), Rng(rng, yBot, yTop));
                canopy.rt.sizeDelta = new Vector2(Rng(rng, 190f, 330f), Rng(rng, 120f, 220f));
                canopy.img.color = new Color(0.12f, Rng(rng, 0.35f, 0.52f), 0.14f, Rng(rng, 0.20f, 0.35f));
                canopy.img.raycastTarget = false;
            }

            for (int i = 0; i < 68; i++)
            {
                float x = Rng(rng, -MapW * 0.48f, MapW * 0.48f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, 0, LevelCatalog.LevelsPerBiome, 110f)) continue;

                float s = Rng(rng, 44f, 92f);
                DrawPineTree(x, y, s, rng);
            }

            for (int i = 0; i < 36; i++)
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
                seg.rt.sizeDelta = new Vector2(28f, segH);
                seg.rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 3.5f) * 18f);
                seg.img.color = new Color(0.18f, 0.48f, 0.72f, 0.50f);
                seg.img.raycastTarget = false;

                var glint = Img("RiverGlint", _contentRT);
                glint.rt.anchoredPosition = new Vector2(x + 6f, y);
                glint.rt.sizeDelta = new Vector2(5f, segH * 0.70f);
                glint.rt.localRotation = seg.rt.localRotation;
                glint.img.color = new Color(0.78f, 0.95f, 1f, 0.35f);
                glint.img.raycastTarget = false;
            }

            DrawBridge(rx + 15f, Mathf.Lerp(p2.y + 80f, p6.y - 50f, 0.47f), -12f);

            for (int i = 0; i < 44; i++)
            {
                float x = Rng(rng, -MapW * 0.47f, MapW * 0.47f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, 0, LevelCatalog.LevelsPerBiome, 70f)) continue;

                var pebble = SprImg("ForestPebble", _contentRT, SpriteFactory.Circle());
                float s = Rng(rng, 6f, 13f);
                pebble.rt.anchoredPosition = new Vector2(x, y);
                pebble.rt.sizeDelta = new Vector2(s * Rng(rng, 1f, 1.8f), s);
                pebble.img.color = new Color(0.28f, 0.23f, 0.16f, Rng(rng, 0.20f, 0.35f));
                pebble.img.raycastTarget = false;
            }
        }

        private void DrawPineTree(float x, float y, float size, System.Random rng)
        {
            var root = new GameObject("PineTree");
            root.transform.SetParent(_contentRT, false);
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0);
            rootRT.anchorMax = new Vector2(0.5f, 0);
            rootRT.pivot = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = new Vector2(x, y);
            rootRT.sizeDelta = Vector2.zero;
            rootRT.localScale = new Vector3(1f, 0.92f, 1f);

            var shadow = SprImg("TreeShadow", _contentRT, SpriteFactory.SoftCircle());
            shadow.rt.anchoredPosition = new Vector2(x, y - size * 0.04f);
            shadow.rt.sizeDelta = new Vector2(size * 0.65f, size * 0.26f);
            shadow.img.color = new Color(0.04f, 0.06f, 0.02f, 0.18f);
            shadow.img.raycastTarget = false;

            var trunk = Img("Trunk", rootRT);
            trunk.rt.anchoredPosition = new Vector2(0, -size * 0.1f);
            trunk.rt.sizeDelta = new Vector2(size * 0.12f, size * 0.30f);
            trunk.img.color = new Color(0.42f, 0.30f, 0.18f, 0.70f);
            trunk.img.raycastTarget = false;

            for (int t = 0; t < 3; t++)
            {
                float layerY = y + size * (0.10f + t * 0.22f);
                float layerW = size * (0.55f - t * 0.12f);
                float layerH = size * 0.38f;

                var outline = SprImg("LeafShade", rootRT, SpriteFactory.Triangle());
                outline.rt.anchoredPosition = new Vector2(0, layerY - y - 2f);
                outline.rt.sizeDelta = new Vector2(layerW * 1.16f, layerH * 1.12f);
                outline.img.color = new Color(0.05f, 0.16f, 0.06f, 0.36f);
                outline.img.raycastTarget = false;

                var leaf = SprImg("Leaf", rootRT, SpriteFactory.Triangle());
                leaf.rt.anchoredPosition = new Vector2(0, layerY - y);
                leaf.rt.sizeDelta = new Vector2(layerW, layerH);
                float green = 0.36f + t * 0.08f + (float)rng.NextDouble() * 0.10f;
                leaf.img.color = new Color(0.12f, green, 0.10f, 0.86f);
                leaf.img.raycastTarget = false;
            }
        }

        private void DrawIceTerrain()
        {
            var rng = new System.Random(99);
            float yTop = NodePos(LevelCatalog.LevelsPerBiome).y + NodeSpacingY * 0.3f;
            float yBot = NodePos(LevelCatalog.TotalLevels - 1).y - NodeSpacingY * 0.4f;

            for (int i = 0; i < 18; i++)
            {
                float x = Rng(rng, -MapW * 0.48f, MapW * 0.48f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, LevelCatalog.LevelsPerBiome, LevelCatalog.TotalLevels, 120f))
                    continue;

                float mW = Rng(rng, 60f, 100f);
                float mH = Rng(rng, 70f, 120f);
                DrawMountain(x, y, mW, mH, rng);
            }

            for (int i = 0; i < 38; i++)
            {
                float x = Rng(rng, -MapW * 0.46f, MapW * 0.46f);
                float y = Rng(rng, yBot, yTop);
                if (TooCloseToNode(x, y, LevelCatalog.LevelsPerBiome, LevelCatalog.TotalLevels, 100f))
                    continue;

                float s = Rng(rng, 40f, 75f);
                DrawIceTree(x, y, s, rng);
            }

            for (int i = 0; i < 70; i++)
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

            for (int i = 0; i < 18; i++)
            {
                float x = Rng(rng, -MapW * 0.40f, MapW * 0.40f);
                float y = Rng(rng, yBot, yTop);
                var patch = SprImg("IcePatch", _contentRT, SpriteFactory.SoftCircle());
                patch.rt.anchoredPosition = new Vector2(x, y);
                patch.rt.sizeDelta = new Vector2(Rng(rng, 80f, 160f), Rng(rng, 30f, 60f));
                patch.img.color = new Color(0.78f, 0.88f, 0.98f, 0.20f);
                patch.img.raycastTarget = false;
            }

            for (int i = 0; i < 14; i++)
            {
                float x = Rng(rng, -MapW * 0.46f, MapW * 0.46f);
                float y = Rng(rng, yBot, yTop);
                DrawIceShard(x, y, Rng(rng, 32f, 68f), rng);
            }
        }

        private void DrawIceTree(float x, float y, float size, System.Random rng)
        {
            var root = new GameObject("IceTree");
            root.transform.SetParent(_contentRT, false);
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0);
            rootRT.anchorMax = new Vector2(0.5f, 0);
            rootRT.pivot = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = new Vector2(x, y);
            rootRT.sizeDelta = Vector2.zero;
            rootRT.localScale = new Vector3(1f, 0.92f, 1f);

            var shadow = SprImg("IceTreeShadow", _contentRT, SpriteFactory.SoftCircle());
            shadow.rt.anchoredPosition = new Vector2(x, y - size * 0.02f);
            shadow.rt.sizeDelta = new Vector2(size * 0.60f, size * 0.20f);
            shadow.img.color = new Color(0.20f, 0.30f, 0.42f, 0.16f);
            shadow.img.raycastTarget = false;

            var trunk = Img("Trunk", rootRT);
            trunk.rt.anchoredPosition = new Vector2(0, -size * 0.1f);
            trunk.rt.sizeDelta = new Vector2(size * 0.10f, size * 0.25f);
            trunk.img.color = new Color(0.50f, 0.48f, 0.45f, 0.55f);
            trunk.img.raycastTarget = false;

            for (int t = 0; t < 3; t++)
            {
                float layerY = y + size * (0.08f + t * 0.20f);
                float layerW = size * (0.48f - t * 0.10f);
                float layerH = size * 0.35f;
                var leaf = SprImg("IceLeaf", rootRT, SpriteFactory.Triangle());
                leaf.rt.anchoredPosition = new Vector2(0, layerY - y);
                leaf.rt.sizeDelta = new Vector2(layerW, layerH);
                float blue = 0.65f + t * 0.08f;
                leaf.img.color = new Color(0.55f, 0.68f + t * 0.05f, blue, 0.65f);
                leaf.img.raycastTarget = false;
            }
        }

        private void DrawMountain(float x, float y, float w, float h, System.Random rng)
        {
            var shade = SprImg("PeakShade", _contentRT, SpriteFactory.Triangle());
            shade.rt.anchoredPosition = new Vector2(x + w * 0.08f, y - h * 0.05f);
            shade.rt.sizeDelta = new Vector2(w * 1.15f, h * 1.08f);
            shade.img.color = new Color(0.20f, 0.22f, 0.30f, 0.22f);
            shade.img.raycastTarget = false;

            var peak = SprImg("Peak", _contentRT, SpriteFactory.Triangle());
            peak.rt.anchoredPosition = new Vector2(x, y);
            peak.rt.sizeDelta = new Vector2(w, h);
            peak.img.color = new Color(0.48f, 0.52f, 0.60f, 0.55f);
            peak.img.raycastTarget = false;

            var cap = SprImg("SnowCap", _contentRT, SpriteFactory.SoftCircle());
            cap.rt.anchoredPosition = new Vector2(x, y + h * 0.65f);
            cap.rt.sizeDelta = new Vector2(w * 0.40f, h * 0.25f);
            cap.img.color = new Color(0.92f, 0.95f, 1f, 0.75f);
            cap.img.raycastTarget = false;
        }

        private void DrawIceShard(float x, float y, float size, System.Random rng)
        {
            var shard = SprImg("IceShard", _contentRT, SpriteFactory.Triangle());
            shard.rt.anchoredPosition = new Vector2(x, y);
            shard.rt.sizeDelta = new Vector2(size * 0.36f, size);
            shard.rt.localRotation = Quaternion.Euler(0, 0, Rng(rng, -16f, 16f));
            shard.img.color = new Color(0.75f, 0.93f, 1f, 0.35f);
            shard.img.raycastTarget = false;
        }

        private void DrawBiomeBorder()
        {
            float forestBotY = NodePos(LevelCatalog.LevelsPerBiome - 1).y;
            float iceTopY = NodePos(LevelCatalog.LevelsPerBiome).y;
            float midY = (forestBotY + iceTopY) * 0.5f;

            var mist = SprImg("BiomeMist", _contentRT, SpriteFactory.SoftCircle());
            mist.rt.anchoredPosition = new Vector2(0, midY);
            mist.rt.sizeDelta = new Vector2(MapW * 1.05f, 260f);
            mist.img.color = new Color(0.80f, 0.88f, 0.86f, 0.20f);
            mist.img.raycastTarget = false;

            var border = Img("BiomeBorder", _contentRT);
            border.rt.anchoredPosition = new Vector2(0, midY - 6f);
            border.rt.sizeDelta = new Vector2(MapW * 0.88f, 8f);
            border.img.color = new Color(0.30f, 0.25f, 0.20f, 0.36f);
            border.img.raycastTarget = false;

            var rng = new System.Random(77);
            for (int i = 0; i < 10; i++)
            {
                float x = -MapW * 0.42f + i * MapW * 0.095f;
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

        private void DrawPaths()
        {
            for (int i = 0; i < LevelCatalog.TotalLevels - 1; i++)
            {
                var p1 = NodePos(i);
                var p2 = NodePos(i + 1);
                bool cross = (i == LevelCatalog.LevelsPerBiome - 1);

                Color shadow = new Color(0.16f, 0.10f, 0.05f, cross ? 0.33f : 0.42f);
                Color road;
                Color center;
                if (cross)
                {
                    road = new Color(0.62f, 0.58f, 0.50f, 0.66f);
                    center = new Color(0.86f, 0.82f, 0.72f, 0.36f);
                }
                else if (i < LevelCatalog.LevelsPerBiome)
                {
                    road = new Color(0.58f, 0.39f, 0.20f, 0.82f);
                    center = new Color(0.90f, 0.72f, 0.42f, 0.38f);
                }
                else
                {
                    road = new Color(0.58f, 0.74f, 0.90f, 0.78f);
                    center = new Color(0.94f, 0.98f, 1f, 0.50f);
                }

                DrawPathLayer(i, p1, p2, shadow, cross ? 24f : 30f, cross ? 10 : 7, "Shadow");
                DrawPathLayer(i, p1, p2, road, cross ? 15f : 21f, cross ? 10 : 7, "Road");
                DrawPathLayer(i, p1, p2, center, cross ? 5f : 7f, cross ? 10 : 7, "Center");
                DrawRoadPebbles(i, p1, p2, i >= LevelCatalog.LevelsPerBiome);
            }
        }

        private void DrawPathLayer(int pathIndex, Vector2 p1, Vector2 p2, Color c, float w, int segs, string suffix)
        {
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

                var seg = Img($"P{pathIndex}_{suffix}_{s}", _contentRT);
                seg.rt.anchoredPosition = new Vector2(mx, my);
                seg.rt.sizeDelta = new Vector2(w, len + 8f);
                seg.rt.localRotation = Quaternion.Euler(0, 0, -angle);
                seg.img.color = c;
                seg.img.raycastTarget = false;
            }
        }

        private void DrawRoadPebbles(int pathIndex, Vector2 p1, Vector2 p2, bool ice)
        {
            var rng = new System.Random(500 + pathIndex);
            for (int s = 1; s <= 3; s++)
            {
                float t = s * 0.25f + Rng(rng, -0.05f, 0.05f);
                var peb = SprImg("RoadPebble", _contentRT, SpriteFactory.Circle());
                peb.rt.anchoredPosition = new Vector2(
                    Mathf.Lerp(p1.x, p2.x, t) + Rng(rng, -16f, 16f),
                    Mathf.Lerp(p1.y, p2.y, t) + Rng(rng, -16f, 16f));
                float size = Rng(rng, 8f, 15f);
                peb.rt.sizeDelta = new Vector2(size * Rng(rng, 1f, 1.6f), size);
                peb.img.color = ice
                    ? new Color(0.82f, 0.95f, 1f, 0.35f)
                    : new Color(0.32f, 0.22f, 0.13f, 0.40f);
                peb.img.raycastTarget = false;
            }
        }

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
                ? new Color(0.34f, 0.66f, 0.96f)
                : new Color(0.50f, 0.76f, 0.26f);

            var shadow = SprImg($"NodeShadow_{lvl}", _contentRT, SpriteFactory.SoftCircle());
            shadow.rt.anchoredPosition = pos + new Vector2(8f, -11f);
            shadow.rt.sizeDelta = new Vector2(NodeRadius * 2.75f, NodeRadius * 2.25f);
            shadow.img.color = new Color(0.05f, 0.035f, 0.02f, 0.32f);
            shadow.img.raycastTarget = false;

            var glow = SprImg($"Glow_{lvl}", _contentRT, SpriteFactory.SoftCircle());
            glow.rt.anchoredPosition = pos;
            glow.rt.sizeDelta = new Vector2(NodeRadius * 3.5f, NodeRadius * 3.5f);
            glow.img.color = new Color(accent.r, accent.g, accent.b, 0.24f);
            glow.img.raycastTarget = false;

            var outline = SprImg($"Outline_{lvl}", _contentRT, SpriteFactory.Circle());
            outline.rt.anchoredPosition = pos;
            outline.rt.sizeDelta = new Vector2(NodeRadius * 2f + 20f, NodeRadius * 2f + 20f);
            outline.img.color = ice ? new Color(0.15f, 0.22f, 0.32f, 0.90f) : new Color(0.24f, 0.15f, 0.08f, 0.92f);
            outline.img.raycastTarget = false;

            var rim = SprImg($"Rim_{lvl}", _contentRT, SpriteFactory.Circle());
            rim.rt.anchoredPosition = pos;
            rim.rt.sizeDelta = new Vector2(NodeRadius * 2f + 10f, NodeRadius * 2f + 10f);
            rim.img.color = ice ? new Color(0.72f, 0.88f, 1f, 0.82f) : new Color(0.96f, 0.72f, 0.30f, 0.86f);
            rim.img.raycastTarget = false;

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

            var highlight = SprImg("NodeHighlight", nodeRT, SpriteFactory.SoftCircle());
            highlight.rt.anchorMin = new Vector2(0.18f, 0.56f);
            highlight.rt.anchorMax = new Vector2(0.68f, 1.00f);
            highlight.rt.offsetMin = Vector2.zero;
            highlight.rt.offsetMax = Vector2.zero;
            highlight.img.color = new Color(1f, 1f, 1f, ice ? 0.24f : 0.18f);
            highlight.img.raycastTarget = false;

            var numGO = new GameObject("Num");
            numGO.transform.SetParent(nodeGO.transform, false);
            var numRT = numGO.AddComponent<RectTransform>();
            numRT.anchorMin = Vector2.zero;
            numRT.anchorMax = Vector2.one;
            numRT.offsetMin = Vector2.zero;
            numRT.offsetMax = Vector2.zero;
            var numTmp = numGO.AddComponent<TextMeshProUGUI>();
            numTmp.text = lvl.ToString();
            numTmp.fontSize = 42;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center;
            numTmp.color = Color.white;
            numTmp.raycastTarget = false;

            var lockGO = new GameObject("Lock");
            lockGO.transform.SetParent(nodeGO.transform, false);
            var lockRT = lockGO.AddComponent<RectTransform>();
            lockRT.anchorMin = Vector2.zero;
            lockRT.anchorMax = Vector2.one;
            lockRT.offsetMin = Vector2.zero;
            lockRT.offsetMax = Vector2.zero;
            var lockTmp = lockGO.AddComponent<TextMeshProUGUI>();
            lockTmp.text = "✕";
            lockTmp.fontSize = 34;
            lockTmp.fontStyle = FontStyles.Bold;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.color = new Color(0.62f, 0.58f, 0.52f, 0.75f);
            lockTmp.raycastTarget = false;

            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(_contentRT, false);
            var nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.5f, 0);
            nameRT.anchorMax = new Vector2(0.5f, 0);
            nameRT.pivot = new Vector2(0.5f, 1f);
            nameRT.anchoredPosition = new Vector2(pos.x, pos.y - NodeRadius - 8f);
            nameRT.sizeDelta = new Vector2(300f, 42f);
            var nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
            nameTmp.text = locName;
            nameTmp.fontSize = 25;
            nameTmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = new Color(0.28f, 0.22f, 0.15f, 0.80f);
            nameTmp.raycastTarget = false;

            var starA = DrawNodeStar(pos + new Vector2(-39f, NodeRadius + 22f), lvl, "A");
            var starB = DrawNodeStar(pos + new Vector2(0f, NodeRadius + 33f), lvl, "B");
            var starC = DrawNodeStar(pos + new Vector2(39f, NodeRadius + 22f), lvl, "C");

            _nodes.Add(new LevelNode
            {
                level = lvl,
                button = btn,
                icon = nodeImg,
                glow = glow.img,
                rim = rim.img,
                starA = starA.img,
                starB = starB.img,
                starC = starC.img,
                numText = numTmp,
                nameText = nameTmp,
                lockText = lockTmp,
            });
        }

        private R DrawNodeStar(Vector2 pos, int lvl, string suffix)
        {
            var star = SprImg($"NodeStar_{lvl}_{suffix}", _contentRT, SpriteFactory.Star());
            star.rt.anchoredPosition = pos;
            star.rt.sizeDelta = new Vector2(30f, 30f);
            star.img.color = new Color(1f, 0.88f, 0.25f, 0.0f);
            star.img.raycastTarget = false;
            return star;
        }

        private void DrawStartCallout()
        {
            var p = NodePos(0);
            Vector2 pos = p + new Vector2(145f, -126f);

            var shadow = Img("StartCalloutShadow", _contentRT);
            shadow.rt.anchoredPosition = pos + new Vector2(5f, -7f);
            shadow.rt.sizeDelta = new Vector2(275f, 78f);
            shadow.img.color = new Color(0.08f, 0.05f, 0.025f, 0.25f);
            shadow.img.raycastTarget = false;

            var bubble = Img("StartCallout", _contentRT);
            bubble.rt.anchoredPosition = pos;
            bubble.rt.sizeDelta = new Vector2(275f, 78f);
            bubble.img.color = new Color(0.96f, 0.90f, 0.72f, 0.94f);
            bubble.img.raycastTarget = false;

            var point = SprImg("StartCalloutPoint", _contentRT, SpriteFactory.Triangle());
            point.rt.anchoredPosition = pos + new Vector2(-88f, 50f);
            point.rt.sizeDelta = new Vector2(44f, 50f);
            point.img.color = bubble.img.color;
            point.img.raycastTarget = false;

            var go = new GameObject("StartCalloutText");
            go.transform.SetParent(_contentRT, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(260f, 66f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "НАЧНИТЕ ЗДЕСЬ!";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.16f, 0.13f, 0.10f, 0.92f);
            tmp.raycastTarget = false;
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
                n.button.transform.localScale = Vector3.one;
                bool done = n.level < _highestUnlocked;
                bool current = n.level == _highestUnlocked;
                bool locked = n.level > _highestUnlocked;

                n.button.interactable = !locked;

                if (done)
                {
                    n.icon.color = new Color(0.30f, 0.80f, 0.35f);
                    n.glow.color = new Color(0.30f, 0.80f, 0.35f, 0.20f);
                    n.rim.color = new Color(1f, 0.88f, 0.35f, 0.86f);
                    n.starA.color = new Color(1f, 0.88f, 0.25f, 0.95f);
                    n.starB.color = new Color(1f, 0.88f, 0.25f, 0.95f);
                    n.starC.color = new Color(1f, 0.88f, 0.25f, 0.95f);
                    n.numText.text = "\u2714";
                    n.numText.fontSize = 44;
                    n.numText.color = Color.white;
                    n.lockText.gameObject.SetActive(false);
                    n.nameText.color = new Color(0.22f, 0.42f, 0.22f, 0.65f);
                }
                else if (current)
                {
                    n.icon.color = new Color(1f, 0.82f, 0.18f);
                    n.glow.color = new Color(1f, 0.82f, 0.18f, 0.35f);
                    n.rim.color = new Color(1f, 0.94f, 0.40f, 0.96f);
                    n.starA.color = new Color(1f, 0.88f, 0.25f, 0.40f);
                    n.starB.color = new Color(1f, 0.88f, 0.25f, 0.40f);
                    n.starC.color = new Color(1f, 0.88f, 0.25f, 0.40f);
                    n.numText.text = n.level.ToString();
                    n.numText.fontSize = 40;
                    n.numText.color = Color.white;
                    n.lockText.gameObject.SetActive(false);
                    n.nameText.color = new Color(0.28f, 0.22f, 0.15f, 0.90f);
                }
                else
                {
                    n.icon.color = new Color(0.22f, 0.20f, 0.19f);
                    n.glow.color = new Color(0.42f, 0.40f, 0.38f, 0.10f);
                    n.rim.color = new Color(0.12f, 0.11f, 0.10f, 0.70f);
                    n.starA.color = new Color(0.40f, 0.38f, 0.34f, 0.0f);
                    n.starB.color = new Color(0.40f, 0.38f, 0.34f, 0.0f);
                    n.starC.color = new Color(0.40f, 0.38f, 0.34f, 0.0f);
                    n.numText.text = n.level.ToString();
                    n.numText.fontSize = 40;
                    n.numText.color = new Color(0.56f, 0.52f, 0.47f);
                    n.lockText.gameObject.SetActive(true);
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
                _nodes[i].button.transform.localScale = Vector3.one;
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n.level != _highestUnlocked) continue;

                float t = Time.unscaledTime;
                float pulse = 0.78f + 0.22f * Mathf.Sin(t * 3.2f);
                n.icon.color = new Color(pulse, 0.82f * pulse, 0.18f * pulse);
                n.rim.color = new Color(1f, 0.95f * pulse, 0.36f, 0.92f);

                float gp = 0.25f + 0.15f * Mathf.Sin(t * 2.5f);
                n.glow.color = new Color(1f, 0.82f, 0.18f, gp);

                float sc = 1f + 0.05f * Mathf.Sin(t * 3.2f);
                n.button.transform.localScale = Vector3.one * sc;
                break;
            }
        }

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

        private void BannerLabel(string text, Vector2 pos, Color textColor, Color bannerColor)
        {
            var shadow = Img("BannerShadow", _contentRT);
            shadow.rt.anchoredPosition = pos + new Vector2(6f, -7f);
            shadow.rt.sizeDelta = new Vector2(500f, 70f);
            shadow.img.color = new Color(0.08f, 0.05f, 0.025f, 0.24f);
            shadow.img.raycastTarget = false;

            var banner = Img("Banner", _contentRT);
            banner.rt.anchoredPosition = pos;
            banner.rt.sizeDelta = new Vector2(500f, 70f);
            banner.img.color = bannerColor;
            banner.img.raycastTarget = false;

            Label(text, 42, textColor, pos + new Vector2(0, 1f));
        }

        private void DrawBridge(float x, float y, float rot)
        {
            var basePlank = Img("BridgeBase", _contentRT);
            basePlank.rt.anchoredPosition = new Vector2(x, y);
            basePlank.rt.sizeDelta = new Vector2(102f, 32f);
            basePlank.rt.localRotation = Quaternion.Euler(0, 0, rot);
            basePlank.img.color = new Color(0.38f, 0.24f, 0.12f, 0.78f);
            basePlank.img.raycastTarget = false;

            for (int i = -2; i <= 2; i++)
            {
                var plank = Img("BridgePlank", _contentRT);
                plank.rt.anchoredPosition = new Vector2(x + i * 20f, y);
                plank.rt.sizeDelta = new Vector2(12f, 42f);
                plank.rt.localRotation = Quaternion.Euler(0, 0, rot);
                plank.img.color = new Color(0.60f, 0.42f, 0.22f, 0.85f);
                plank.img.raycastTarget = false;
            }
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
