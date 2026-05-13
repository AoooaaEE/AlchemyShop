using StickEvolve.Combat;
using UnityEngine;

namespace StickEvolve.Wave
{
    /// <summary>
    /// Создаёт GameObject врага из EnemyKind. Внешний вид — стикмен из примитивов (StickmanBuilder).
    /// </summary>
    public static class EnemyFactory
    {
        public static Enemy Spawn(EnemyKind kind, Vector3 pos, WaveConfig cfg)
        {
            var go = new GameObject($"Enemy_{kind}");
            go.transform.position = pos;

            BuildVisual(go, kind);

            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 1.4f);
            col.isTrigger = true;

            go.AddComponent<TeamMember>();
            var hp = go.AddComponent<Health>();
            var enemy = go.AddComponent<Enemy>();
            enemy.kind = kind;

            ApplyStats(enemy, hp, kind, cfg);
            return enemy;
        }

        private static void BuildVisual(GameObject root, EnemyKind kind)
        {
            var color = TeamColor(kind);
            var cfg = StickmanConfig.Default(color);

            switch (kind)
            {
                case EnemyKind.Fighter:
                    cfg.bodyScale = 1f;
                    cfg.limbThickness = 0.15f;
                    break;
                case EnemyKind.Runner:
                    cfg.bodyScale = 1.05f;
                    cfg.limbThickness = 0.11f;
                    cfg.legLength = 0.65f;
                    cfg.armLength = 0.6f;
                    cfg.torsoHeight = 0.55f;
                    cfg.headSize = 0.36f;
                    break;
                case EnemyKind.Tank:
                    cfg.bodyScale = 1.15f;
                    cfg.limbThickness = 0.22f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.48f;
                    cfg.torsoHeight = 0.7f;
                    cfg.legLength = 0.5f;
                    break;
                case EnemyKind.Mage:
                    cfg.bodyScale = 1f;
                    cfg.limbThickness = 0.14f;
                    cfg.hasHat = true;
                    cfg.hatColor = new Color(0.4f, 0.15f, 0.55f);
                    cfg.headSize = 0.44f;
                    break;
                case EnemyKind.Boss:
                    cfg.bodyScale = 1.7f;
                    cfg.limbThickness = 0.22f;
                    cfg.wideShoulders = true;
                    cfg.headSize = 0.55f;
                    cfg.torsoHeight = 0.8f;
                    cfg.legLength = 0.6f;
                    cfg.armLength = 0.7f;
                    break;
            }

            StickmanBuilder.Build(root, cfg);
        }

        private static Color TeamColor(EnemyKind kind) => kind switch
        {
            EnemyKind.Fighter => new Color(0.85f, 0.3f, 0.3f),
            EnemyKind.Runner  => new Color(1f, 0.55f, 0.2f),
            EnemyKind.Tank    => new Color(0.55f, 0.25f, 0.25f),
            EnemyKind.Mage    => new Color(0.7f, 0.35f, 0.95f),
            EnemyKind.Boss    => new Color(0.45f, 0.08f, 0.08f),
            _ => Color.red,
        };

        private static void ApplyStats(Enemy e, Health hp, EnemyKind kind, WaveConfig cfg)
        {
            switch (kind)
            {
                case EnemyKind.Fighter:
                    hp.Configure(6f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.6f;
                    e.damage = 1f * cfg.enemyDamageMultiplier;
                    e.attackRate = 1f;
                    e.attackRange = 0.7f;
                    e.goldDrop = cfg.enemyGoldDrop;
                    break;
                case EnemyKind.Runner:
                    hp.Configure(3f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 3.0f;
                    e.damage = 0.7f * cfg.enemyDamageMultiplier;
                    e.attackRate = 1.5f;
                    e.attackRange = 0.6f;
                    e.goldDrop = cfg.enemyGoldDrop;
                    break;
                case EnemyKind.Tank:
                    hp.Configure(20f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.9f;
                    e.damage = 2f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.6f;
                    e.attackRange = 0.8f;
                    e.goldDrop = cfg.enemyGoldDrop * 3;
                    break;
                case EnemyKind.Mage:
                    hp.Configure(5f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 1.1f;
                    e.damage = 0f;
                    e.attackRate = 0.7f;
                    e.attackRange = 0.7f;
                    e.bulletDamage = 1.5f * cfg.enemyDamageMultiplier;
                    e.bulletSpeed = 6f;
                    e.goldDrop = cfg.enemyGoldDrop * 2;
                    break;
                case EnemyKind.Boss:
                    hp.Configure(80f * cfg.enemyHpMultiplier);
                    e.moveSpeed = 0.7f;
                    e.damage = 4f * cfg.enemyDamageMultiplier;
                    e.attackRate = 0.5f;
                    e.attackRange = 1.0f;
                    e.goldDrop = cfg.enemyGoldDrop * 25;
                    break;
            }
        }
    }
}
