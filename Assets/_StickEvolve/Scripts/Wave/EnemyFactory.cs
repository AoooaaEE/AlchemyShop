using StickEvolve.Combat;
using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Wave
{
    /// <summary>
    /// Создаёт GameObject врага из EnemyKind. Внешний вид — стикмен из примитивов (StickmanBuilder).
    /// </summary>
    public static class EnemyFactory
    {
        /// <summary>Последний WaveConfig, использованный для спавна. Используется Splitter-ом для миньонов.</summary>
        private static WaveConfig _lastSpawnCfg;

        public static Enemy Spawn(EnemyKind kind, Vector3 pos, WaveConfig cfg)
        {
            _lastSpawnCfg = cfg;
            return SpawnInternal(kind, pos, cfg, splitTier: kind == EnemyKind.Splitter ? 2 : 0);
        }

        /// <summary>Мини-копия для Splitter-ов: половинный HP, без дальнейшего деления, без шапки.</summary>
        public static Enemy SpawnMini(Vector3 pos, int splitTier)
        {
            var cfg = _lastSpawnCfg ?? new WaveConfig
            {
                enemyHpMultiplier = 1f, enemyDamageMultiplier = 1f, enemyGoldDrop = 1,
            };
            return SpawnInternal(EnemyKind.Splitter, pos, cfg, splitTier);
        }

        private static Enemy SpawnInternal(EnemyKind kind, Vector3 pos, WaveConfig cfg, int splitTier)
        {
            var go = new GameObject($"Enemy_{kind}");
            go.transform.position = pos;

            BuildVisual(go, kind, splitTier);
            AddShadow(go);
            AddShielderShield(go, kind);

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.4f);
            col.isTrigger = true;

            go.AddComponent<TeamMember>();
            var hp = go.AddComponent<Health>();
            var enemy = go.AddComponent<Enemy>();
            enemy.kind = kind;
            enemy.splitTier = splitTier;

            ApplyStats(enemy, hp, kind, cfg, splitTier);
            return enemy;
        }

        private static void BuildVisual(GameObject root, EnemyKind kind, int splitTier)
        {
            var color = TeamColor(kind);
            var cfg = StickmanConfig.Default(color);
            // Глаза у врагов — красные, чтобы они визуально отличались от героев.
            cfg.eyeColor = new Color(0.95f, 0.15f, 0.15f);
            cfg.accentColor = new Color(0.15f, 0.04f, 0.04f);
            cfg.weaponColor = new Color(0.58f, 0.56f, 0.52f);

            switch (kind)
            {
                case EnemyKind.Fighter:
                    cfg.bodyScale = 1f;
                    cfg.limbThickness = 0.15f;
                    cfg.weapon = StickmanWeapon.Sword;
                    cfg.accentColor = new Color(0.38f, 0.05f, 0.05f);
                    break;
                case EnemyKind.Runner:
                    cfg.bodyScale = 1.05f;
                    cfg.limbThickness = 0.11f;
                    cfg.legLength = 0.65f;
                    cfg.armLength = 0.6f;
                    cfg.torsoHeight = 0.55f;
                    cfg.headSize = 0.36f;
                    cfg.weapon = StickmanWeapon.Dagger;
                    cfg.hasMask = true;
                    cfg.accentColor = new Color(0.30f, 0.08f, 0.02f);
                    break;
                case EnemyKind.Tank:
                    cfg.bodyScale = 1.15f;
                    cfg.limbThickness = 0.22f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.48f;
                    cfg.torsoHeight = 0.7f;
                    cfg.legLength = 0.5f;
                    cfg.handSize = 0.16f;
                    cfg.weapon = StickmanWeapon.Shield;
                    cfg.hasShoulderPads = true;
                    cfg.accentColor = new Color(0.32f, 0.20f, 0.15f);
                    break;
                case EnemyKind.Mage:
                    cfg.bodyScale = 1f;
                    cfg.limbThickness = 0.14f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(0.4f, 0.15f, 0.55f);
                    cfg.headSize = 0.44f;
                    cfg.hasCape = true;
                    cfg.capeColor = new Color(0.35f, 0.10f, 0.5f);
                    cfg.weapon = StickmanWeapon.Staff;
                    cfg.accentColor = new Color(0.95f, 0.22f, 0.95f);
                    cfg.hasAura = true;
                    cfg.auraColor = new Color(0.70f, 0.10f, 0.95f, 0.18f);
                    break;
                case EnemyKind.Boss:
                    cfg.bodyScale = 1.7f;
                    cfg.limbThickness = 0.22f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.55f;
                    cfg.torsoHeight = 0.8f;
                    cfg.legLength = 0.6f;
                    cfg.armLength = 0.7f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(0.25f, 0.02f, 0.02f);
                    cfg.hasCape = true;
                    cfg.capeColor = new Color(0.30f, 0.05f, 0.05f);
                    cfg.handSize = 0.18f;
                    cfg.eyeColor = new Color(1f, 0.5f, 0f);
                    cfg.weapon = StickmanWeapon.Axe;
                    cfg.hasShoulderPads = true;
                    cfg.hasCrown = true;
                    cfg.accentColor = new Color(1f, 0.58f, 0.05f);
                    cfg.hasAura = true;
                    cfg.auraColor = new Color(0.9f, 0.05f, 0.02f, 0.20f);
                    break;
                case EnemyKind.Healer:
                    cfg.bodyScale = 0.95f;
                    cfg.limbThickness = 0.13f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(1f, 1f, 1f);
                    cfg.headSize = 0.42f;
                    cfg.hasCape = true;
                    cfg.capeColor = new Color(0.85f, 0.90f, 0.80f);
                    cfg.eyeColor = new Color(0.35f, 0.85f, 0.45f);
                    cfg.weapon = StickmanWeapon.Staff;
                    cfg.weaponColor = new Color(0.65f, 0.55f, 0.35f);
                    cfg.accentColor = new Color(0.55f, 1f, 0.55f);
                    cfg.hasAura = true;
                    cfg.auraColor = new Color(0.40f, 1f, 0.55f, 0.17f);
                    break;
                case EnemyKind.Shielder:
                    cfg.bodyScale = 1.1f;
                    cfg.limbThickness = 0.2f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.46f;
                    cfg.torsoHeight = 0.7f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(0.35f, 0.40f, 0.50f);
                    cfg.weapon = StickmanWeapon.Shield;
                    cfg.accentColor = new Color(0.70f, 0.74f, 0.86f);
                    cfg.hasShoulderPads = true;
                    break;
                case EnemyKind.Splitter:
                    float mul = splitTier >= 2 ? 1.1f : 0.65f;
                    cfg.bodyScale = mul;
                    cfg.limbThickness = 0.14f * mul;
                    cfg.headSize = 0.42f * mul;
                    cfg.weapon = splitTier >= 2 ? StickmanWeapon.Sword : StickmanWeapon.Dagger;
                    cfg.hasMask = splitTier < 2;
                    cfg.accentColor = new Color(0.95f, 0.25f, 0.45f);
                    break;
                case EnemyKind.Sniper:
                    cfg.bodyScale = 0.95f;
                    cfg.limbThickness = 0.12f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(0.2f, 0.25f, 0.4f);
                    cfg.armLength = 0.65f;
                    cfg.raiseRightArm = true;
                    cfg.weapon = StickmanWeapon.Rifle;
                    cfg.weaponColor = new Color(0.12f, 0.14f, 0.16f);
                    cfg.accentColor = new Color(0.40f, 0.55f, 0.92f);
                    break;
                case EnemyKind.Bomber:
                    cfg.bodyScale = 1.05f;
                    cfg.limbThickness = 0.18f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.48f;
                    cfg.bodyColor = new Color(0.95f, 0.65f, 0.2f);
                    cfg.skinColor = new Color(0.9f, 0.6f, 0.2f);
                    cfg.eyeColor = new Color(1f, 0.95f, 0f); // ядовито-жёлтые глаза
                    cfg.handSize = 0.14f;
                    cfg.weapon = StickmanWeapon.Bomb;
                    cfg.weaponColor = new Color(0.06f, 0.06f, 0.07f);
                    cfg.accentColor = new Color(1f, 0.85f, 0.20f);
                    cfg.hasAura = true;
                    cfg.auraColor = new Color(1f, 0.55f, 0.05f, 0.16f);
                    break;
            }

            StickmanBuilder.Build(root, cfg);
        }

        /// <summary>Эллиптическая тень под персонажем.</summary>
        private static void AddShadow(GameObject root)
        {
            var s = new GameObject("Shadow");
            s.transform.SetParent(root.transform, false);
            s.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            s.transform.localScale = new Vector3(0.85f, 0.22f, 1f);
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.SoftCircle();
            sr.color = new Color(0f, 0f, 0f, 0.5f);
            sr.sortingOrder = 1;
        }

        /// <summary>Визуальный щит у Shielder-а спереди (слева, т.к. он смотрит на героев).</summary>
        private static void AddShielderShield(GameObject root, EnemyKind kind)
        {
            if (kind != EnemyKind.Shielder) return;
            var sh = new GameObject("Shield");
            sh.transform.SetParent(root.transform, false);
            sh.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            sh.transform.localScale = new Vector3(0.22f, 1.1f, 1f);
            var sr = sh.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White();
            sr.color = new Color(0.3f, 0.35f, 0.4f);
            sr.sortingOrder = 6;
        }

        private static Color TeamColor(EnemyKind kind) => kind switch
        {
            EnemyKind.Fighter  => new Color(0.85f, 0.3f, 0.3f),
            EnemyKind.Runner   => new Color(1f, 0.55f, 0.2f),
            EnemyKind.Tank     => new Color(0.55f, 0.25f, 0.25f),
            EnemyKind.Mage     => new Color(0.7f, 0.35f, 0.95f),
            EnemyKind.Boss     => new Color(0.45f, 0.08f, 0.08f),
            EnemyKind.Healer   => new Color(0.85f, 0.95f, 0.85f),
            EnemyKind.Shielder => new Color(0.55f, 0.55f, 0.7f),
            EnemyKind.Splitter => new Color(0.95f, 0.45f, 0.6f),
            EnemyKind.Sniper   => new Color(0.4f, 0.5f, 0.75f),
            EnemyKind.Bomber   => new Color(0.95f, 0.65f, 0.2f),
            _ => Color.red,
        };

        private static void ApplyStats(Enemy e, Health hp, EnemyKind kind, WaveConfig cfg, int splitTier)
        {
            switch (kind)
            {
                case EnemyKind.Fighter:
                    hp.Configure(5f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.4f;
                    e.damage = 1f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.8f;
                    e.attackRange = 0.7f;
                    e.goldDrop = cfg.enemyGoldDrop;
                    break;
                case EnemyKind.Runner:
                    hp.Configure(2.5f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 2.6f;
                    e.damage = 0.6f * cfg.enemyDamageMultiplier;
                    e.attackRate = 1.2f;
                    e.attackRange = 0.6f;
                    e.goldDrop = cfg.enemyGoldDrop;
                    break;
                case EnemyKind.Tank:
                    hp.Configure(16f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.8f;
                    e.damage = 1.8f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.5f;
                    e.attackRange = 0.8f;
                    e.goldDrop = cfg.enemyGoldDrop * 3;
                    break;
                case EnemyKind.Mage:
                    hp.Configure(4f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.0f;
                    e.damage = 0f;
                    e.attackRate = 0.6f;
                    e.attackRange = 0.7f;
                    e.bulletDamage = 1.0f * cfg.enemyDamageMultiplier;
                    e.bulletSpeed = 6f;
                    e.goldDrop = cfg.enemyGoldDrop * 2;
                    break;
                case EnemyKind.Boss:
                    hp.Configure(90f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.65f;
                    e.damage = 3.5f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.5f;
                    e.attackRange = 1.0f;
                    e.goldDrop = cfg.enemyGoldDrop * 25;
                    break;
                case EnemyKind.Healer:
                    hp.Configure(6f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.3f;
                    e.damage = 0f;
                    e.attackRate = 0.9f;
                    e.attackRange = 4.5f;
                    e.bulletDamage = 1.8f * cfg.enemyDamageMultiplier; // = heal amount
                    e.bulletSpeed = 7f;
                    e.bulletColor = new Color(0.4f, 1f, 0.5f);
                    e.goldDrop = cfg.enemyGoldDrop * 3;
                    break;
                case EnemyKind.Shielder:
                    hp.Configure(14f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.9f;
                    e.damage = 1.2f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.6f;
                    e.attackRange = 0.7f;
                    hp.damageReductionFlat = 0.6f;
                    e.damageReductionFlat = 0.6f;
                    e.goldDrop = cfg.enemyGoldDrop * 3;
                    break;
                case EnemyKind.Splitter:
                    float tierMul = splitTier >= 2 ? 1f : 0.4f;
                    hp.Configure(6f * tierMul * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.6f + (splitTier >= 2 ? 0f : 0.6f);
                    e.damage = 0.9f * cfg.enemyDamageMultiplier;
                    e.attackRate = 1f;
                    e.attackRange = 0.7f;
                    e.goldDrop = splitTier >= 2 ? cfg.enemyGoldDrop * 2 : cfg.enemyGoldDrop;
                    break;
                case EnemyKind.Sniper:
                    hp.Configure(3.5f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.7f;
                    e.damage = 0f;
                    e.attackRate = 0.35f;
                    e.attackRange = 1f;
                    e.bulletDamage = 2.8f * cfg.enemyDamageMultiplier;
                    e.bulletSpeed = 11f;
                    e.bulletColor = new Color(0.6f, 0.7f, 1f);
                    e.goldDrop = cfg.enemyGoldDrop * 3;
                    break;
                case EnemyKind.Bomber:
                    hp.Configure(5f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 2.2f;
                    e.damage = 3.5f * cfg.enemyDamageMultiplier;
                    e.attackRate = 1f;
                    e.attackRange = 0.7f;
                    e.bulletExplosionRadius = 1.6f;
                    e.selfDestructOnAttack = true;
                    e.goldDrop = cfg.enemyGoldDrop * 2;
                    break;
            }
        }
    }
}
