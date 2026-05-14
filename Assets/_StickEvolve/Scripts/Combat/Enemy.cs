using System.Collections.Generic;
using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    public enum EnemyKind
    {
        Fighter,
        Runner,
        Tank,
        Mage,
        Boss,
        Healer,
        Shielder,
        Splitter,
        Sniper,
        Bomber,
    }

    /// <summary>
    /// Базовый враг. Идёт справа налево к ближайшему герою, при контакте бьёт.
    /// Mage / Sniper стреляют. Healer лечит союзников. Bomber подбегает и взрывается.
    /// Splitter делится на 2 мелких при смерти. При смерти роняет золото.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class Enemy : MonoBehaviour
    {
        [Header("Тип")]
        public EnemyKind kind = EnemyKind.Fighter;
        public int splitTier;     // для Splitter: 2 = крупный (делится), 1 = мелкий (не делится)

        [Header("Движение")]
        public float moveSpeed = 1.5f;
        public float attackRange = 0.7f;

        [Header("Атака")]
        public float damage = 1f;
        public float attackRate = 1f;
        public float bulletSpeed = 6f;
        public float bulletDamage = 1f;
        public float bulletExplosionRadius;     // Bomber / магичные AoE
        public float bulletExplosionSplash = 0.7f;
        public bool selfDestructOnAttack;       // Bomber

        [Header("Защита")]
        public float damageReductionFlat;       // 0..1 — % срез урона у Shielder

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

            // Healer ведёт себя иначе: ищет раненого союзника и лечит его.
            if (kind == EnemyKind.Healer)
            {
                HealerUpdate();
                return;
            }

            _target = FindNearestHero();
            if (_target == null) return;

            Vector3 toTarget = _target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            float effectiveRange = GetEffectiveRange();

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

        private float GetEffectiveRange()
        {
            switch (kind)
            {
                case EnemyKind.Mage:   return attackRange + 4f;
                case EnemyKind.Sniper: return attackRange + 7f;
                default:               return attackRange;
            }
        }

        private void HealerUpdate()
        {
            // Двигается к самому раненому союзнику, держится позади.
            var ally = FindMostInjuredAlly();
            if (ally == null)
            {
                // Никого нет рядом — медленно идёт влево, как обычный враг.
                transform.position += Vector3.left * (moveSpeed * Time.deltaTime);
                return;
            }

            Vector3 to = ally.transform.position - transform.position;
            float dist = to.magnitude;
            float keepDist = 2.5f;
            if (dist > keepDist + 0.2f)
            {
                Vector3 dir = dist > 0.001f ? (to / dist) : Vector3.left;
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }
            else if (dist < keepDist - 0.2f)
            {
                Vector3 dir = dist > 0.001f ? -(to / dist) : Vector3.right;
                transform.position += dir * (moveSpeed * 0.5f * Time.deltaTime);
            }

            if (Time.time < _nextAttackTime) return;
            if (attackRate <= 0f) return;
            _nextAttackTime = Time.time + 1f / attackRate;

            Vector2 fdir = (ally.transform.position - transform.position).normalized;
            if (fdir.sqrMagnitude < 0.001f) fdir = Vector2.left;
            var b = Bullet.Spawn(transform.position + (Vector3)(fdir * 0.4f), fdir, bulletDamage, bulletSpeed,
                CombatTeam.Enemies, new Color(0.4f, 1f, 0.5f));
            b.isHealing = true;
        }

        private void TryAttack()
        {
            if (Time.time < _nextAttackTime) return;
            if (attackRate <= 0f) return;
            _nextAttackTime = Time.time + 1f / attackRate;

            if (_target == null) return;

            switch (kind)
            {
                case EnemyKind.Mage:
                case EnemyKind.Sniper:
                {
                    Vector2 dir = (_target.transform.position - transform.position).normalized;
                    var b = Bullet.Spawn(transform.position + (Vector3)(dir * 0.4f), dir, bulletDamage, bulletSpeed,
                        CombatTeam.Heroes, bulletColor);
                    b.explosionRadius = bulletExplosionRadius;
                    b.explosionSplashRatio = bulletExplosionSplash;
                    break;
                }
                case EnemyKind.Bomber:
                {
                    // Самоподрыв в AoE
                    ApplyBomberExplosion();
                    if (selfDestructOnAttack)
                        Health.TakeDamage(99999f, transform.position);
                    break;
                }
                default:
                {
                    var dmg = _target.GetComponent<IDamageable>();
                    if (dmg != null && dmg.IsAlive)
                    {
                        dmg.TakeDamage(damage, _target.transform.position);
                        DamageNumber.Spawn(_target.transform.position, damage, new Color(1f, 0.3f, 0.3f));

                        // Шипы: герой возвращает урон ближнему атакующему.
                        var hero = _target.GetComponent<Hero>();
                        if (hero != null && hero.thornsDamage > 0f && Health != null && Health.IsAlive)
                        {
                            Health.TakeDamage(hero.thornsDamage, transform.position);
                            DamageNumber.Spawn(transform.position, hero.thornsDamage, new Color(0.6f, 0.9f, 1f));
                        }
                    }
                    break;
                }
            }
        }

        private void ApplyBomberExplosion()
        {
            float r = Mathf.Max(0.5f, bulletExplosionRadius);
            Bullet.SpawnExplosionFx(transform.position, r);
            var heroes = HeroRegistry.Instance.Alive;
            for (int i = heroes.Count - 1; i >= 0; i--)
            {
                var h = heroes[i];
                if (h == null) continue;
                float d = (h.transform.position - transform.position).sqrMagnitude;
                if (d > r * r) continue;
                var dmg = h.GetComponent<IDamageable>();
                if (dmg == null || !dmg.IsAlive) continue;
                dmg.TakeDamage(damage, h.transform.position);
                DamageNumber.Spawn(h.transform.position, damage, new Color(1f, 0.5f, 0.2f));
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

        private Enemy FindMostInjuredAlly()
        {
            var all = EnemyRegistry.Instance.Alive;
            Enemy best = null;
            float worstRatio = 1f;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || e == this) continue;
                if (!e.IsAlive) continue;
                var hp = e.Health;
                if (hp == null || hp.MaxHp <= 0f) continue;
                float ratio = hp.CurrentHp / hp.MaxHp;
                if (ratio < worstRatio && ratio < 0.95f)
                {
                    worstRatio = ratio;
                    best = e;
                }
            }
            return best;
        }

        private void HandleDeath()
        {
            float mult = Mathf.Max(0.1f, StickEvolve.Cards.CardProgression.GoldMultiplier);
            int payout = Mathf.Max(1, Mathf.RoundToInt(goldDrop * mult));
            StickGameRefs.Economy?.AddGold(payout);
            GoldDrop.Spawn(transform.position, payout);
            StickEvolve.Core.StickGame.Instance?.NotifyEnemyKilled();

            // Splitter: на смерти крупного спавним 2 мелких рядом, без дальнейшего деления.
            if (kind == EnemyKind.Splitter && splitTier >= 2)
            {
                Wave.EnemyFactory.SpawnMini(transform.position + new Vector3(0f, 0.3f, 0f), splitTier - 1);
                Wave.EnemyFactory.SpawnMini(transform.position + new Vector3(0f, -0.3f, 0f), splitTier - 1);
            }

            // Запускаем анимацию падения, после неё гасим GameObject.
            DeathFallAnimator.Begin(gameObject);
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
