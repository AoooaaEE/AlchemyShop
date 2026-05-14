using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    public enum StickmanWeapon
    {
        None,
        Sword,
        Bow,
        Staff,
        Shield,
        Dagger,
        Rifle,
        Axe,
        Bomb,
    }

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
        public StickmanWeapon weapon;
        public Color weaponColor;
        public Color accentColor;
        public bool hasShoulderPads;
        public bool hasMask;
        public bool hasCrown;
        public bool hasBackQuiver;
        public bool hasChestSigil;
        public bool hasAura;
        public Color auraColor;

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
                weapon = StickmanWeapon.None,
                weaponColor = new Color(0.75f, 0.75f, 0.82f),
                accentColor = Color.white,
                hasShoulderPads = false,
                hasMask = false,
                hasCrown = false,
                hasBackQuiver = false,
                hasChestSigil = true,
                hasAura = false,
                auraColor = new Color(body.r, body.g, body.b, 0.22f),
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

            if (cfg.hasAura)
            {
                MakeCircle(parent.transform, "Aura", cfg.auraColor,
                    new Vector3(0f, torsoCenterY, 0.08f),
                    new Vector3(1.5f, 1.9f, 1f),
                    sortingOrder: 1);
            }

            // — Плащ (за торсом) —
            if (cfg.hasCape)
            {
                MakeRect(parent.transform, "Cape", cfg.capeColor,
                    new Vector3(0f, torsoCenterY - cfg.torsoHeight * 0.05f, 0.01f),
                    new Vector3(torsoWidth * 1.3f, cfg.torsoHeight * 1.15f, 1f),
                    sortingOrder: 2);
            }

            if (cfg.hasBackQuiver)
            {
                var quiver = MakeRect(parent.transform, "Quiver", new Color(0.28f, 0.16f, 0.08f),
                    new Vector3(-torsoWidth * 0.75f, torsoCenterY + 0.02f, 0.02f),
                    new Vector3(cfg.limbThickness * 0.75f, cfg.torsoHeight * 0.9f, 1f),
                    sortingOrder: 2);
                quiver.localRotation = Quaternion.Euler(0f, 0f, -24f);
                for (int i = 0; i < 3; i++)
                {
                    var arrow = MakeRect(parent.transform, $"QuiverArrow_{i}", new Color(0.85f, 0.74f, 0.48f),
                        new Vector3(-torsoWidth * 0.75f + i * 0.035f, torsoCenterY + cfg.torsoHeight * 0.52f, 0.01f),
                        new Vector3(0.025f, cfg.torsoHeight * 0.45f, 1f),
                        sortingOrder: 2);
                    arrow.localRotation = Quaternion.Euler(0f, 0f, -24f);
                }
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

            if (cfg.hasChestSigil)
            {
                MakeCircle(parent.transform, "ChestSigil", cfg.accentColor,
                    new Vector3(0f, torsoCenterY + cfg.torsoHeight * 0.12f, -0.01f),
                    Vector3.one * Mathf.Max(0.055f, torsoWidth * 0.28f),
                    sortingOrder: 5);
            }

            if (cfg.hasShoulderPads)
            {
                MakeCircle(parent.transform, "ShoulderPadL", cfg.accentColor,
                    new Vector3(-torsoWidth * 0.72f, shoulderY, -0.01f),
                    new Vector3(cfg.limbThickness * 1.8f, cfg.limbThickness * 1.3f, 1f),
                    sortingOrder: 5);
                MakeCircle(parent.transform, "ShoulderPadR", cfg.accentColor,
                    new Vector3(torsoWidth * 0.72f, shoulderY, -0.01f),
                    new Vector3(cfg.limbThickness * 1.8f, cfg.limbThickness * 1.3f, 1f),
                    sortingOrder: 5);
            }

            // — Голова —
            var head = MakeCircle(parent.transform, "Head", cfg.skinColor,
                new Vector3(0f, headCenterY, 0f),
                Vector3.one * cfg.headSize,
                sortingOrder: 5);

            if (cfg.hasMask)
            {
                MakeRect(head, "Mask", cfg.accentColor,
                    new Vector3(0f, 0.08f, -0.02f),
                    new Vector3(0.72f, 0.23f, 1f),
                    sortingOrder: 7);
            }

            // — Шапка / шлем —
            if (cfg.hasHat)
            {
                MakeRect(parent.transform, "Hat", cfg.hatColor,
                    new Vector3(0f, headCenterY + cfg.headSize * 0.55f, 0f),
                    new Vector3(cfg.headSize * 1.2f, cfg.headSize * 0.55f, 1f),
                    sortingOrder: 6);
            }

            if (cfg.hasCrown)
            {
                for (int i = 0; i < 3; i++)
                {
                    var crown = MakeTriangle(parent.transform, $"CrownPeak_{i}", cfg.accentColor,
                        new Vector3((i - 1) * cfg.headSize * 0.22f, headCenterY + cfg.headSize * 0.78f, -0.02f),
                        new Vector3(cfg.headSize * 0.22f, cfg.headSize * 0.25f, 1f),
                        sortingOrder: 8);
                }
                MakeRect(parent.transform, "CrownBand", cfg.accentColor,
                    new Vector3(0f, headCenterY + cfg.headSize * 0.58f, -0.02f),
                    new Vector3(cfg.headSize * 0.95f, cfg.headSize * 0.12f, 1f),
                    sortingOrder: 8);
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

            AddWeapon(parent.transform, armL, armR, cfg);

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

        private static Transform MakeTriangle(Transform parent, string name, Color color, Vector3 pos, Vector3 scale, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Triangle();
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

        private static void AddWeapon(Transform root, Transform armL, Transform armR, StickmanConfig cfg)
        {
            switch (cfg.weapon)
            {
                case StickmanWeapon.Sword:
                    MakeRect(armR, "SwordBlade", cfg.weaponColor,
                        new Vector3(0f, -cfg.armLength - 0.24f, -0.03f),
                        new Vector3(0.075f, 0.62f, 1f), sortingOrder: 9);
                    MakeRect(armR, "SwordGuard", cfg.accentColor,
                        new Vector3(0f, -cfg.armLength - 0.03f, -0.04f),
                        new Vector3(0.28f, 0.045f, 1f), sortingOrder: 9);
                    break;
                case StickmanWeapon.Bow:
                    for (int i = 0; i < 3; i++)
                    {
                        var seg = MakeRect(armL, $"Bow_{i}", cfg.weaponColor,
                            new Vector3(-0.10f, -cfg.armLength * 0.58f + (i - 1) * 0.18f, -0.03f),
                            new Vector3(0.045f, 0.34f, 1f), sortingOrder: 9);
                        seg.localRotation = Quaternion.Euler(0f, 0f, i == 1 ? 0f : (i == 0 ? -22f : 22f));
                    }
                    MakeRect(armL, "BowString", new Color(0.93f, 0.88f, 0.72f),
                        new Vector3(-0.20f, -cfg.armLength * 0.58f, -0.04f),
                        new Vector3(0.018f, 0.82f, 1f), sortingOrder: 9);
                    break;
                case StickmanWeapon.Staff:
                    MakeRect(armR, "StaffShaft", cfg.weaponColor,
                        new Vector3(0.02f, -cfg.armLength - 0.20f, -0.03f),
                        new Vector3(0.055f, 0.86f, 1f), sortingOrder: 9);
                    MakeCircle(armR, "StaffOrb", cfg.accentColor,
                        new Vector3(0.02f, -cfg.armLength - 0.66f, -0.04f),
                        Vector3.one * 0.22f, sortingOrder: 10);
                    break;
                case StickmanWeapon.Shield:
                    MakeCircle(armL, "HeldShield", cfg.accentColor,
                        new Vector3(-0.08f, -cfg.armLength * 0.72f, -0.05f),
                        new Vector3(0.42f, 0.56f, 1f), sortingOrder: 9);
                    MakeCircle(armL, "ShieldBoss", cfg.weaponColor,
                        new Vector3(-0.08f, -cfg.armLength * 0.72f, -0.06f),
                        Vector3.one * 0.16f, sortingOrder: 10);
                    break;
                case StickmanWeapon.Dagger:
                    for (int i = 0; i < 2; i++)
                    {
                        var blade = MakeRect(i == 0 ? armL : armR, $"Dagger_{i}", cfg.weaponColor,
                            new Vector3(0f, -cfg.armLength - 0.10f, -0.03f),
                            new Vector3(0.055f, 0.32f, 1f), sortingOrder: 9);
                        blade.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? -18f : 18f);
                    }
                    break;
                case StickmanWeapon.Rifle:
                    var rifle = MakeRect(armR, "RifleBarrel", cfg.weaponColor,
                        new Vector3(0.18f, -cfg.armLength - 0.05f, -0.03f),
                        new Vector3(0.13f, 0.82f, 1f), sortingOrder: 9);
                    rifle.localRotation = Quaternion.Euler(0f, 0f, 86f);
                    MakeRect(armR, "RifleStock", cfg.accentColor,
                        new Vector3(-0.18f, -cfg.armLength - 0.02f, -0.04f),
                        new Vector3(0.16f, 0.26f, 1f), sortingOrder: 9);
                    break;
                case StickmanWeapon.Axe:
                    MakeRect(armR, "AxeHandle", cfg.weaponColor,
                        new Vector3(0f, -cfg.armLength - 0.22f, -0.03f),
                        new Vector3(0.06f, 0.68f, 1f), sortingOrder: 9);
                    var axe = MakeTriangle(armR, "AxeHead", cfg.accentColor,
                        new Vector3(0.14f, -cfg.armLength - 0.52f, -0.04f),
                        new Vector3(0.28f, 0.28f, 1f), sortingOrder: 10);
                    axe.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    break;
                case StickmanWeapon.Bomb:
                    MakeCircle(armR, "BombBody", cfg.weaponColor,
                        new Vector3(0f, -cfg.armLength - 0.10f, -0.03f),
                        Vector3.one * 0.28f, sortingOrder: 9);
                    var fuse = MakeRect(armR, "BombFuse", cfg.accentColor,
                        new Vector3(0.09f, -cfg.armLength - 0.26f, -0.04f),
                        new Vector3(0.025f, 0.20f, 1f), sortingOrder: 10);
                    fuse.localRotation = Quaternion.Euler(0f, 0f, -35f);
                    break;
            }
        }
    }
}
