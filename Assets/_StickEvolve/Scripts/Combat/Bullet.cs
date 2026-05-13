using StickEvolve.Economy;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Простой снаряд: летит по direction со speed, наносит damage первому IDamageable
    /// в команде targetTeam. Сам себя убивает по maxLifetime или при попадании.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        public float damage = 1f;
        public float speed = 12f;
        public Vector2 direction = Vector2.right;
        public CombatTeam targetTeam = CombatTeam.Enemies;
        public float maxLifetime = 3f;
        public float critChance = 0f;
        public float critMultiplier = 2f;
        public float explosionRadius = 0f;
        public float explosionSplashRatio = 0.6f;
        public bool isHealing;          // true — лечит союзника (Healer)
        public Hero ownerHero;          // для крит-триггеров (напр. Ninja-клон)

        private float _age;

        public static Bullet Spawn(Vector3 pos, Vector2 dir, float dmg, float spd, CombatTeam team, Color color)
        {
            var go = new GameObject("Bullet");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.White();
            sr.color = color;
            sr.sortingOrder = 5;
            go.transform.localScale = new Vector3(0.35f, 0.12f, 1f);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var b = go.AddComponent<Bullet>();
            b.damage = dmg;
            b.speed = spd;
            b.direction = dir.normalized;
            b.targetTeam = team;
            return b;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var team = other.GetComponent<TeamMember>() ?? other.GetComponentInParent<TeamMember>();
            if (team == null || team.team != targetTeam) return;

            if (isHealing)
            {
                ApplyHeal(other);
                Destroy(gameObject);
                return;
            }

            var dmg = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (dmg == null || !dmg.IsAlive) return;

            float final = damage;
            bool crit = false;
            if (critChance > 0f && Random.value < critChance)
            {
                final *= critMultiplier;
                crit = true;
            }
            dmg.TakeDamage(final, transform.position);
            DamageNumber.Spawn(transform.position, final, crit ? new Color(1f, 0.7f, 0.2f) : Color.white);

            if (crit && ownerHero != null) ownerHero.OnCrit(transform.position);

            if (explosionRadius > 0f)
                ApplySplash(other, final * explosionSplashRatio);

            Destroy(gameObject);
        }

        private void ApplyHeal(Collider2D ally)
        {
            var hp = ally.GetComponent<Health>() ?? ally.GetComponentInParent<Health>();
            if (hp == null || !hp.IsAlive) return;
            hp.Heal(damage);
            DamageNumber.Spawn(transform.position, damage, new Color(0.4f, 1f, 0.5f));
        }

        private void ApplySplash(Collider2D primaryTarget, float splashDamage)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var o = hits[i];
                if (o == null || o == primaryTarget) continue;
                var team = o.GetComponent<TeamMember>() ?? o.GetComponentInParent<TeamMember>();
                if (team == null || team.team != targetTeam) continue;
                var d = o.GetComponent<IDamageable>() ?? o.GetComponentInParent<IDamageable>();
                if (d == null || !d.IsAlive) continue;
                d.TakeDamage(splashDamage, transform.position);
            }
            SpawnExplosionFx(transform.position, explosionRadius);
        }

        public static void SpawnExplosionFx(Vector3 pos, float radius)
        {
            var go = new GameObject("ExplosionFx");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radius * 2f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle();
            sr.color = new Color(1f, 0.8f, 0.3f, 0.55f);
            sr.sortingOrder = 4;
            var fx = go.AddComponent<ExplosionFxFade>();
            fx.lifetime = 0.25f;
        }
    }

    public class ExplosionFxFade : MonoBehaviour
    {
        public float lifetime = 0.25f;
        private float _age;
        private SpriteRenderer _sr;
        private Vector3 _startScale;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _startScale = transform.localScale;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / lifetime);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = Mathf.Lerp(0.55f, 0f, t);
                _sr.color = c;
            }
            transform.localScale = _startScale * Mathf.Lerp(0.8f, 1.2f, t);
            if (_age >= lifetime) Destroy(gameObject);
        }
    }
}
