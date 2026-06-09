using System;
using StickEvolve.VFX;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Авто-стрелок-герой. Стоит на месте, каждые 1/fireRate секунд ищет ближайшего врага
    /// в пределах range и стреляет в него. Имеет HP — если все герои умирают, GameOver.
    /// Healer лечит союзников вместо урона. Berserker наносит больше урона на низком HP.
    /// Ninja при крите спавнит временного клона.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class Hero : MonoBehaviour
    {
        [Header("Класс")]
        public HeroClass HeroClass = HeroClass.Warrior;

        [Header("Боевые параметры")]
        public float damage = 1f;
        public float fireRate = 1.5f;
        public float range = 7f;
        public float bulletSpeed = 14f;
        public float critChance = 0f;
        public float critMultiplier = 2f;
        public float bulletExplosionRadius = 0f;

        [Header("Класс-специфика")]
        public bool isHealer;
        public float berserkerScale;
        public float ninjaCloneChance;
        public bool isClone;
        public float cloneLifetime = 5f;

        [Header("Глобальные модификаторы")]
        public int multiShot;       // +N дополнительных снарядов за выстрел
        public int bulletPierce;    // у пуль есть пробитие
        public float lifesteal;     // 0..1, доля урона возвращается в HP
        public float thornsDamage;  // ответный урон врагу в ближнем бою

        [Header("Визуал")]
        public Color bulletColor = new Color(0.4f, 0.8f, 1f);

        /// <summary>Глобальный спавнер клонов; задаётся бутстрапером.</summary>
        public static Func<HeroClass, Vector3, Hero> CloneSpawnerFunc;

        private float _nextFireTime;
        private float _nextCloneTime;
        private float _cloneDespawnTime = -1f;
        private Health _health;
        private StickmanAnimator _animator;
        private Character3DAnimator _animator3D;

        private void Awake()
        {
            _health = GetComponent<Health>();
            var team = GetComponent<TeamMember>();
            team.team = CombatTeam.Heroes;
            _animator = GetComponentInChildren<StickmanAnimator>();
            _animator3D = GetComponentInChildren<Character3DAnimator>();
            _health.OnDeath += HandleOwnDeath;
        }

        private void HandleOwnDeath()
        {
            // Клон не входит в постоянный отряд; на смерти просто исчезает.
            if (isClone) DeathFallAnimator.Begin(gameObject);
        }

        private void OnEnable() => HeroRegistry.Instance.Register(this);
        private void OnDisable() => HeroRegistry.Instance.Unregister(this);

        public void MarkAsClone(float lifetime)
        {
            isClone = true;
            cloneLifetime = lifetime;
            _cloneDespawnTime = Time.time + lifetime;
        }

        public void OnCrit(Vector3 atPos)
        {
            // Ninja: при крите шанс спавна клона (не делается клонами).
            if (!isClone && ninjaCloneChance > 0f && Time.time >= _nextCloneTime
                && UnityEngine.Random.value < ninjaCloneChance)
            {
                _nextCloneTime = Time.time + 3f;
                if (CloneSpawnerFunc != null)
                {
                    var offset = new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f),
                                             UnityEngine.Random.Range(-0.6f, 0.6f), 0f);
                    var clone = CloneSpawnerFunc.Invoke(HeroClass, transform.position + offset);
                    if (clone != null) clone.MarkAsClone(cloneLifetime);
                }
            }
        }

        private void Update()
        {
            if (!_health.IsAlive) return;

            // Клон сам умирает по таймеру.
            if (isClone && _cloneDespawnTime > 0f && Time.time >= _cloneDespawnTime)
            {
                _health.TakeDamage(99999f, transform.position);
                return;
            }

            if (fireRate <= 0f) return;
            if (Time.time < _nextFireTime) return;

            if (isHealer)
            {
                if (TryHealAlly()) _nextFireTime = Time.time + 1f / fireRate;
                return;
            }

            var target = FindNearestEnemy();
            if (target == null) return;

            Vector2 dir = target.transform.position - transform.position;
            float dist = dir.magnitude;
            if (dist > range) return;

            dir = dist > 0.001f ? (dir / dist) : Vector2.right;
            _nextFireTime = Time.time + 1f / fireRate;

            float finalDmg = damage * BerserkerMultiplier();
            int totalShots = 1 + Mathf.Max(0, multiShot);
            // Базовая стрельба: один снаряд по direction; экстра-снаряды раздаются с +-spread по углу.
            for (int i = 0; i < totalShots; i++)
            {
                Vector2 shotDir = dir;
                if (totalShots > 1)
                {
                    // Распределяем углы равномерно от -spreadHalf до +spreadHalf вокруг базового направления
                    float spreadHalf = 8f * Mathf.Min(totalShots - 1, 3); // макс ±24°
                    float t = (totalShots == 1) ? 0f : (i / (float)(totalShots - 1)) * 2f - 1f; // -1..+1
                    float angle = t * spreadHalf;
                    float rad = angle * Mathf.Deg2Rad;
                    float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
                    shotDir = new Vector2(dir.x * cs - dir.y * sn, dir.x * sn + dir.y * cs);
                }
                var b = Bullet.Spawn(transform.position + (Vector3)(shotDir * 0.4f), shotDir, finalDmg, bulletSpeed,
                    CombatTeam.Enemies, bulletColor);
                b.critChance = critChance;
                b.critMultiplier = critMultiplier;
                b.explosionRadius = bulletExplosionRadius;
                b.piercesLeft = bulletPierce;
                b.lifestealRatio = lifesteal;
                b.ownerHero = this;
            }
            _animator?.TriggerShoot();
            _animator3D?.TriggerAttack(target.transform.position);
        }

        private float BerserkerMultiplier()
        {
            if (berserkerScale <= 0f || _health == null || _health.MaxHp <= 0f) return 1f;
            float ratio = Mathf.Clamp01(_health.CurrentHp / _health.MaxHp);
            return 1f + berserkerScale * (1f - ratio);
        }

        private bool TryHealAlly()
        {
            var ally = FindMostInjuredAlly();
            if (ally == null) return false;
            Vector3 to = ally.transform.position - transform.position;
            float dist = to.magnitude;
            if (dist > range) return false;
            Vector2 dir = dist > 0.001f ? (Vector2)(to / dist) : Vector2.right;
            var b = Bullet.Spawn(transform.position + (Vector3)(dir * 0.4f), dir, damage, bulletSpeed,
                CombatTeam.Heroes, new Color(0.4f, 1f, 0.5f));
            b.isHealing = true;
            _animator?.TriggerShoot();
            _animator3D?.TriggerAttack(ally.transform.position);
            return true;
        }

        private Hero FindMostInjuredAlly()
        {
            var all = HeroRegistry.Instance.Alive;
            Hero best = null;
            float worstRatio = 1f;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (h == null || h == this) continue;
                var hp = h.GetComponent<Health>();
                if (hp == null || !hp.IsAlive || hp.MaxHp <= 0f) continue;
                float ratio = hp.CurrentHp / hp.MaxHp;
                if (ratio < worstRatio && ratio < 0.95f)
                {
                    worstRatio = ratio;
                    best = h;
                }
            }
            return best;
        }

        private Enemy FindNearestEnemy()
        {
            var all = EnemyRegistry.Instance.Alive;
            Enemy best = null;
            float bestSq = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e == null || !e.IsAlive) continue;
                float d = (e.transform.position - transform.position).sqrMagnitude;
                if (d < bestSq)
                {
                    bestSq = d;
                    best = e;
                }
            }
            return best;
        }
    }
}
