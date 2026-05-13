using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Конфиг для собираемого «стикмена» из примитивов: голова, торс, 2 руки, 2 ноги.
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

        public static StickmanConfig Default(Color body)
        {
            return new StickmanConfig
            {
                bodyColor = body,
                skinColor = new Color(body.r * 0.85f, body.g * 0.85f, body.b * 0.85f, 1f),
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
            var torso = MakeRect(parent.transform, "Torso", cfg.bodyColor,
                new Vector3(0f, torsoCenterY, 0f),
                new Vector3(torsoWidth, cfg.torsoHeight, 1f),
                sortingOrder: 3);

            var head = MakeCircle(parent.transform, "Head", cfg.skinColor,
                new Vector3(0f, headCenterY, 0f),
                Vector3.one * cfg.headSize,
                sortingOrder: 5);

            if (cfg.hasHat)
            {
                MakeRect(parent.transform, "Hat", cfg.hatColor,
                    new Vector3(0f, headCenterY + cfg.headSize * 0.55f, 0f),
                    new Vector3(cfg.headSize * 1.2f, cfg.headSize * 0.55f, 1f),
                    sortingOrder: 6);
            }

            float hipHalf = cfg.limbThickness * 0.6f;
            var legL = MakeLimbPivot(parent.transform, "LegL",
                new Vector3(-hipHalf, hipY, 0f),
                cfg.bodyColor, cfg.legLength, cfg.limbThickness, sortingOrder: 2);
            var legR = MakeLimbPivot(parent.transform, "LegR",
                new Vector3(hipHalf, hipY, 0f),
                cfg.bodyColor, cfg.legLength, cfg.limbThickness, sortingOrder: 2);

            float shoulderHalf = (cfg.wideShoulders ? cfg.limbThickness * 1.3f : cfg.limbThickness * 0.9f);
            var armL = MakeLimbPivot(parent.transform, "ArmL",
                new Vector3(-shoulderHalf, shoulderY, 0f),
                cfg.bodyColor, cfg.armLength, cfg.limbThickness * 0.85f, sortingOrder: 4);
            var armR = MakeLimbPivot(parent.transform, "ArmR",
                new Vector3(shoulderHalf, shoulderY, 0f),
                cfg.bodyColor, cfg.armLength, cfg.limbThickness * 0.85f, sortingOrder: 4);

            if (cfg.raiseRightArm)
                armR.localRotation = Quaternion.Euler(0f, 0f, 75f);

            var anim = parent.AddComponent<StickmanAnimator>();
            anim.torso = torso;
            anim.head = head;
            anim.legL = legL;
            anim.legR = legR;
            anim.armL = armL;
            anim.armR = armR;
            anim.keepRightArmRaised = cfg.raiseRightArm;
            anim.SetTorsoBaseY(torsoCenterY);

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
