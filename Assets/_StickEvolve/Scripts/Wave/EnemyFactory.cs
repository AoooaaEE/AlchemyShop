using StickEvolve.Combat;
using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Wave
{
    /// <summary>
    /// Создаёт GameObject врага из EnemyKind. Все «прото-арты» — цветные прямоугольники.
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
            // Тело (прямоугольник)
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var bsr = body.AddComponent<SpriteRenderer>();
            bsr.sprite = SpriteFactory.White();
            bsr.color = TeamColor(kind);
            bsr.sortingOrder = 3;
            body.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            body.transform.localScale = BodyScale(kind);

            // Голова (круг)
            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            var hsr = head.AddComponent<SpriteRenderer>();
            hsr.sprite = SpriteFactory.Circle();
            hsr.color = TeamColor(kind) * 0.85f;
            hsr.sortingOrder = 4;
            head.transform.localPosition = new Vector3(0f, 0.55f, 0f) + HeadOffset(kind);
            head.transform.localScale = HeadScale(kind);
        }

        private static Color TeamColor(EnemyKind kind) => kind switch
        {
            EnemyKind.Fighter => new Color(0.85f, 0.3f, 0.3f),
            EnemyKind.Runner  => new Color(1f, 0.55f, 0.2f),
            EnemyKind.Tank    => new Color(0.5f, 0.25f, 0.25f),
            EnemyKind.Mage    => new Color(0.8f, 0.4f, 0.95f),
            EnemyKind.Boss    => new Color(0.4f, 0.05f, 0.05f),
            _ => Color.red,
        };

        private static Vector3 BodyScale(EnemyKind kind) => kind switch
        {
            EnemyKind.Tank => new Vector3(0.9f, 1.1f, 1f),
            EnemyKind.Boss => new Vector3(1.5f, 1.8f, 1f),
            EnemyKind.Runner => new Vector3(0.4f, 0.85f, 1f),
            _ => new Vector3(0.5f, 0.9f, 1f),
        };

        private static Vector3 HeadScale(EnemyKind kind) => kind switch
        {
            EnemyKind.Tank => new Vector3(0.55f, 0.55f, 1f),
            EnemyKind.Boss => new Vector3(0.9f, 0.9f, 1f),
            _ => new Vector3(0.4f, 0.4f, 1f),
        };

        private static Vector3 HeadOffset(EnemyKind kind) => kind switch
        {
            EnemyKind.Boss => new Vector3(0f, 0.45f, 0f),
            _ => Vector3.zero,
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
