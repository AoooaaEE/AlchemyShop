using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Конфиг для собираемого «стикмена» из примитивов: голова, торс, 2 руки, 2 ноги,
    /// + детали (глаза, кисти, ботинки, ремень, плащ).
    /// Поза покоя = руки вниз, ноги ровно. При движении StickmanAnimator качает их sin-волной.
    /// </summary>
    public struct StickmanConfig
    {
        public Color bodyColor;
        public Color skinColor;
        public float bodyScale;
        public float limbThickness;
        public float headSize;
        public float torsoHeight;
        public float legLength;
        public float armLength;
        public bool wideShoulders;
        public bool raiseRightArm;
        public bool hasHat;
        public Color hatColor;

        // Новые детали
        public bool hasEyes;          // 2 чёрных точки на голове
        public Color eyeColor;
        public bool hasBelt;          // полоса на торсе у пояса
        public Color beltColor;
        public bool hasCape;          // плащ позади торса (для магов/боссов)
        public Color capeColor;
        public float handSize;        // 0 — без кистей. Иначе круг на конце руки.
        public Color handColor;
        public float footSize;        // 0 — без ботинок. Иначе тёмный прямоугольник под ногой.
        public Color footColor;

        public static StickmanConfig Default(Color body)
        {
            var skin = new Color(body.r * 0.85f, body.g * 0.85f, body.b * 0.85f, 1f);
            return new StickmanConfig
            {
                bodyColor = body,
                skinColor = skin,
                bodyScale = 1f,
                limbThickness = 0.14f,
                headSize = 0.42f,
                torsoHeight = 0.62f,
                legLength = 0.55f,
                armLength = 0.55f,
                wideShoulders = false,
                raiseRightArm = false,
                hasHat = false,
                hatColor = Color.black,

                hasEyes = true,
                eyeColor = new Color(0.05f, 0.05f, 0.08f),
                hasBelt = true,
                beltColor = new Color(0.15f, 0.10f, 0.06f),
                hasCape = false,
                capeColor = new Color(body.r * 0.55f, body.g * 0.55f, body.b * 0.7f, 1f),
                handSize = 0.10f,
                handColor = skin,
                footSize = 0.12f,
                footColor = new Color(0.10f, 0.08f, 0.05f),
            };
        }
    }

    /// <summary>
    /// Собирает иерархию «стикмена» как детей переданного GameObject и навешивает StickmanAnimator.
    /// Все примитивы — белый/круглый спрайт через SpriteFactory, тонированный цветом.
    /// </summary>
    public static class StickmanBuilder
    {
        public static StickmanAnimator Build(GameObject parent, StickmanConfig cfg)
        {
            float totalH = cfg.legLength + cfg.torsoHeight + cfg.headSize;
            float yOff = -totalH * 0.5f;

            float hipY = yOff + cfg.legLength;
            float torsoCenterY = hipY + cfg.torsoHeight * 0.5f;
            float shoulderY = hipY + cfg.torsoHeight * 0.85f;
            float headCenterY = hipY + cfg.torsoHeight + cfg.headSize * 0.5f;

            float torsoWidth = (cfg.wideShoulders ? cfg.limbThickness * 2.8f : cfg.limbThickness * 2f);

            // — Плащ (за торсом) —
            if (cfg.hasCape)
            {
                MakeRect(parent.transform, "Cape", cfg.capeColor,
                    new Vector3(0f, torsoCenterY - cfg.torsoHeight * 0.05f, 0.01f),
                    new Vector3(torsoWidth * 1.3f, cfg.torsoHeight * 1.15f, 1f),
                    sortingOrder: 2);
            }

            // — Торс —
            var torso = MakeRect(parent.transform, "Torso", cfg.bodyColor,
                new Vector3(0f, torsoCenterY, 0f),
                new Vector3(torsoWidth, cfg.torsoHeight, 1f),
                sortingOrder: 3);

            // — Ремень —
            if (cfg.hasBelt)
            {
                MakeRect(parent.transform, "Belt", cfg.beltColor,
                    new Vector3(0f, hipY + cfg.torsoHeight * 0.18f, 0f),
                    new Vector3(torsoWidth * 1.05f, cfg.torsoHeight * 0.12f, 1f),
                    sortingOrder: 4);
            }

            // — Голова —
            var head = MakeCircle(parent.transform, "Head", cfg.skinColor,
                new Vector3(0f, headCenterY, 0f),
                Vector3.one * cfg.headSize,
                sortingOrder: 5);

            // — Шапка / шлем —
            if (cfg.hasHat)
            {
                MakeRect(parent.transform, "Hat", cfg.hatColor,
                    new Vector3(0f, headCenterY + cfg.headSize * 0.55f, 0f),
                    new Vector3(cfg.headSize * 1.2f, cfg.headSize * 0.55f, 1f),
                    sortingOrder: 6);
            }

            // — Глаза (дети head, поэтому позиция/масштаб в head-локали: head.localScale = headSize) —
            Transform eyeL = null, eyeR = null;
            if (cfg.hasEyes)
            {
                eyeL = MakeCircle(head, "EyeL", cfg.eyeColor,
                    new Vector3(-0.18f, 0.08f, -0.01f),
                    Vector3.one * 0.18f,
                    sortingOrder: 7);
                eyeR = MakeCircle(head, "EyeR", cfg.eyeColor,
                    new Vector3(0.18f, 0.08f, -0.01f),
                    Vector3.one * 0.18f,
                    sortingOrder: 7);
            }

            // — Ноги (с ботинками) —
            float hipHalf = cfg.limbThickness * 0.6f;
            var legL = MakeLimbPivot(parent.transform, "LegL",
                new Vector3(-hipHalf, hipY, 0f),
                cfg.bodyColor, cfg.legLength, cfg.limbThickness, sortingOrder: 2);
            var legR = MakeLimbPivot(parent.transform, "LegR",
                new Vector3(hipHalf, hipY, 0f),
                cfg.bodyColor, cfg.legLength, cfg.limbThickness, sortingOrder: 2);

            if (cfg.footSize > 0f)
            {
                AddTipBlob(legL, "FootL", cfg.footColor,
                    new Vector3(0f, -cfg.legLength, 0f),
                    new Vector3(cfg.footSize * 1.6f, cfg.footSize, 1f),
                    isCircle: false, sortingOrder: 2);
                AddTipBlob(legR, "FootR", cfg.footColor,
                    new Vector3(0f, -cfg.legLength, 0f),
                    new Vector3(cfg.footSize * 1.6f, cfg.footSize, 1f),
                    isCircle: false, sortingOrder: 2);
            }

            // — Руки (с кистями) —
            float shoulderHalf = (cfg.wideShoulders ? cfg.limbThickness * 1.3f : cfg.limbThickness * 0.9f);
            var armL = MakeLimbPivot(parent.transform, "ArmL",
                new Vector3(-shoulderHalf, shoulderY, 0f),
                cfg.bodyColor, cfg.armLength, cfg.limbThickness * 0.85f, sortingOrder: 4);
            var armR = MakeLimbPivot(parent.transform, "ArmR",
                new Vector3(shoulderHalf, shoulderY, 0f),
                cfg.bodyColor, cfg.armLength, cfg.limbThickness * 0.85f, sortingOrder: 4);

            if (cfg.handSize > 0f)
            {
                AddTipBlob(armL, "HandL", cfg.handColor,
                    new Vector3(0f, -cfg.armLength, 0f),
                    Vector3.one * cfg.handSize,
                    isCircle: true, sortingOrder: 4);
                AddTipBlob(armR, "HandR", cfg.handColor,
                    new Vector3(0f, -cfg.armLength, 0f),
                    Vector3.one * cfg.handSize,
                    isCircle: true, sortingOrder: 4);
            }

            // — Поза покоя: правая рука поднята (для геройских стрелков) —
            if (cfg.raiseRightArm)
                armR.localRotation = Quaternion.Euler(0f, 0f, 75f);

            // — Аниматор —
            var anim = parent.AddComponent<StickmanAnimator>();
            anim.torso = torso;
            anim.head = head;
            anim.legL = legL;
            anim.legR = legR;
            anim.armL = armL;
            anim.armR = armR;
            anim.eyeL = eyeL;
            anim.eyeR = eyeR;
            anim.keepRightArmRaised = cfg.raiseRightArm;
            anim.SetTorsoBaseY(torsoCenterY);
            anim.SetHeadBaseY(headCenterY);

            parent.transform.localScale = Vector3.one * cfg.bodyScale;
            return anim;
        }

        private static Transform MakeRect(Transform parent, string name, Color color, Vector3 pos, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        private static Transform MakeCircle(Transform parent, string name, Color color, Vector3 pos, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go.transform;
        }

        /// <summary>Маленький круг или прямоугольник на конце конечности.</summary>
        private static void AddTipBlob(Transform limbPivot, string name, Color color, Vector3 pos, Vector3 scale, bool isCircle, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(limbPivot, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isCircle ? SpriteFactory.Circle() : SpriteFactory.White();
            sr.color = color;
            sr.sortingOrder = sortingOrder + 1;
        }

        private static Transform MakeLimbPivot(Transform parent, string name, Vector3 pivotPos, Color color, float length, float thickness, int sortingOrder)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;

            var spr = new GameObject("Sprite");
            spr.transform.SetParent(pivot.transform, false);
            spr.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            spr.transform.localScale = new Vector3(thickness, length, 1f);
            var sr = spr.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White();
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            return pivot.transform;
        }
    }
}
