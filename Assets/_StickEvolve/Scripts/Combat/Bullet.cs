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
            Destroy(gameObject);
        }
    }
}
