using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Авто-стрелок-герой. Стоит на месте, каждые 1/fireRate секунд ищет ближайшего врага
    /// в пределах range и стреляет в него. Имеет HP — если все герои умирают, GameOver.
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

        [Header("Визуал")]
        public Color bulletColor = new Color(0.4f, 0.8f, 1f);

        private float _nextFireTime;
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
            var team = GetComponent<TeamMember>();
            team.team = CombatTeam.Heroes;
        }

        private void OnEnable() => HeroRegistry.Instance.Register(this);
        private void OnDisable() => HeroRegistry.Instance.Unregister(this);

        private void Update()
        {
            if (!_health.IsAlive) return;
            if (fireRate <= 0f) return;
            if (Time.time < _nextFireTime) return;

            var target = FindNearestEnemy();
            if (target == null) return;

            Vector2 dir = target.transform.position - transform.position;
            float dist = dir.magnitude;
            if (dist > range) return;

            dir = dist > 0.001f ? (dir / dist) : Vector2.right;
            _nextFireTime = Time.time + 1f / fireRate;

            var b = Bullet.Spawn(transform.position + (Vector3)(dir * 0.4f), dir, damage, bulletSpeed, CombatTeam.Enemies, bulletColor);
            b.critChance = critChance;
            b.critMultiplier = critMultiplier;
            b.explosionRadius = bulletExplosionRadius;
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
