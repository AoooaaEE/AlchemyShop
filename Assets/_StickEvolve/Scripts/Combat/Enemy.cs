using System.Collections.Generic;
using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    public enum EnemyKind { Fighter, Runner, Tank, Mage, Boss }

    /// <summary>
    /// Базовый враг. Идёт справа налево к ближайшему герою, при контакте бьёт.
    /// Mage стреляет вместо ближнего боя. При смерти роняет золото.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class Enemy : MonoBehaviour
    {
        [Header("Тип")]
        public EnemyKind kind = EnemyKind.Fighter;

        [Header("Движение")]
        public float moveSpeed = 1.5f;
        public float attackRange = 0.7f;

        [Header("Атака")]
        public float damage = 1f;
        public float attackRate = 1f;
        public float bulletSpeed = 6f;
        public float bulletDamage = 1f;

        [Header("Дроп")]
        public int goldDrop = 1;

        [Header("Визуал")]
        public Color bulletColor = new Color(1f, 0.4f, 0.4f);

        public Health Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;

        private float _nextAttackTime;
        private Hero _target;

        private void Awake()
        {
            Health = GetComponent<Health>();
            var team = GetComponent<TeamMember>();
            team.team = CombatTeam.Enemies;
            Health.OnDeath += HandleDeath;
        }

        private void OnEnable() => EnemyRegistry.Instance.Register(this);
        private void OnDisable() => EnemyRegistry.Instance.Unregister(this);

        private void Update()
        {
            if (!IsAlive) return;
            _target = FindNearestHero();
            if (_target == null) return;

            Vector3 toTarget = _target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            float effectiveRange = kind == EnemyKind.Mage ? attackRange + 4f : attackRange;

            if (dist > effectiveRange)
            {
                Vector3 dir = dist > 0.001f ? (toTarget / dist) : Vector3.left;
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }
            else
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackTime) return;
            if (attackRate <= 0f) return;
            _nextAttackTime = Time.time + 1f / attackRate;

            if (kind == EnemyKind.Mage && _target != null)
            {
                Vector2 dir = (_target.transform.position - transform.position).normalized;
                Bullet.Spawn(transform.position + (Vector3)(dir * 0.4f), dir, bulletDamage, bulletSpeed, CombatTeam.Heroes, bulletColor);
            }
            else if (_target != null)
            {
                var dmg = _target.GetComponent<IDamageable>();
                if (dmg != null && dmg.IsAlive)
                {
                    dmg.TakeDamage(damage, _target.transform.position);
                    DamageNumber.Spawn(_target.transform.position, damage, new Color(1f, 0.3f, 0.3f));
                }
            }
        }

        private Hero FindNearestHero()
        {
            var all = HeroRegistry.Instance.Alive;
            Hero best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (h == null) continue;
                var hp = h.GetComponent<Health>();
                if (hp == null || !hp.IsAlive) continue;
                float d = (h.transform.position - transform.position).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = h;
                }
            }
            return best;
        }

        private void HandleDeath()
        {
            StickGameRefs.Economy?.AddGold(goldDrop);
            GoldDrop.Spawn(transform.position, goldDrop);
            Destroy(gameObject, 0.05f);
        }
    }

    /// <summary>Реестр живых врагов для O(1) поиска.</summary>
    public class EnemyRegistry
    {
        private static EnemyRegistry _instance;
        public static EnemyRegistry Instance => _instance ??= new EnemyRegistry();
        public readonly List<Enemy> Alive = new();
        public void Register(Enemy e) { if (!Alive.Contains(e)) Alive.Add(e); }
        public void Unregister(Enemy e) { Alive.Remove(e); }
        public void Clear() => Alive.Clear();
    }

    /// <summary>Реестр живых героев.</summary>
    public class HeroRegistry
    {
        private static HeroRegistry _instance;
        public static HeroRegistry Instance => _instance ??= new HeroRegistry();
        public readonly List<Hero> Alive = new();
        public void Register(Hero h) { if (!Alive.Contains(h)) Alive.Add(h); }
        public void Unregister(Hero h) { Alive.Remove(h); }
        public void Clear() => Alive.Clear();
    }
}
