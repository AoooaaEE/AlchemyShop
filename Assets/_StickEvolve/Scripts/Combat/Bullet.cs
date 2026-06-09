using StickEvolve.Economy;
using StickEvolve.VFX;
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
        public Hero ownerHero;          // для крит-триггеров (напр. Ninja-клон) и lifesteal
        public int piercesLeft;         // 0 = снаряд исчезает после первого попадания
        public float lifestealRatio;    // 0..1, доля урона возвращается в HP стрелка

        private float _age;
        private readonly System.Collections.Generic.HashSet<int> _hitColliderIds = new();

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

            Build3DProjectileVisual(go.transform, color);

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
            StickAudio.PlayShoot(pos);
            return b;
        }

        private static void Build3DProjectileVisual(Transform parent, Color color)
        {
            // Старая пуля — плоский SpriteRenderer. Для перспективной 3D-камеры добавляем
            // эмиссивное ядро + короткий trail + маленький point light. Коллайдеры/физику не трогаем.
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "BulletCore3D";
            var coreCol = core.GetComponent<Collider>();
            if (coreCol != null) Destroy(coreCol);
            core.transform.SetParent(parent, worldPositionStays: false);
            core.transform.localPosition = new Vector3(0f, 0f, 0.28f);
            // Parent у старой 2D-пули уже растянут (0.35 x 0.12), поэтому компенсируем scale,
            // чтобы ядро не превратилось в плоскую иголку.
            core.transform.localScale = new Vector3(0.62f, 1.80f, 0.22f);
            var mr = core.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LitMaterial.GetEmissive(color, 2.4f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var trail = parent.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.14f;
            trail.widthMultiplier = 0.16f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.minVertexDistance = 0.03f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.material = LitMaterial.GetEmissive(color, 1.4f);
            trail.startColor = new Color(color.r, color.g, color.b, 0.95f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

            var lgo = new GameObject("BulletLight3D");
            lgo.transform.SetParent(parent, worldPositionStays: false);
            lgo.transform.localPosition = new Vector3(0f, 0f, 0.30f);
            var light = lgo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 0.45f;
            light.range = 1.4f;
            light.shadows = LightShadows.None;
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

            // Защита от повторного попадания в того же врага при пробивании
            int colliderId = other.GetInstanceID();
            if (_hitColliderIds.Contains(colliderId)) return;
            _hitColliderIds.Add(colliderId);

            float final = damage;
            bool crit = false;
            if (critChance > 0f && Random.value < critChance)
            {
                final *= critMultiplier;
                crit = true;
            }
            dmg.TakeDamage(final, transform.position);
            Color hitColor = crit ? new Color(1f, 0.75f, 0.15f) : new Color(0.75f, 0.9f, 1f);
            DamageNumber.Spawn(transform.position, final, crit ? new Color(1f, 0.7f, 0.2f) : Color.white, crit);
            ImpactBurst3D.Spawn(transform.position, hitColor, count: crit ? 14 : 8, speed: crit ? 4.8f : 3.4f, lifetime: crit ? 0.42f : 0.30f, size: crit ? 0.10f : 0.075f);
            if (crit)
            {
                StickAudio.PlayCrit(transform.position);
                HitStop.Punch(0.055f, 0.065f);
            }

            if (crit && ownerHero != null) ownerHero.OnCrit(transform.position);

            if (lifestealRatio > 0f && ownerHero != null)
            {
                var ownerHp = ownerHero.GetComponent<Health>();
                if (ownerHp != null && ownerHp.IsAlive)
                    ownerHp.Heal(final * lifestealRatio);
            }

            if (explosionRadius > 0f)
                ApplySplash(other, final * explosionSplashRatio);

            // Пробитие: если снаряд должен прошить ещё одного врага — не уничтожаем.
            if (piercesLeft > 0)
            {
                piercesLeft--;
                return;
            }

            Destroy(gameObject);
        }

        private void ApplyHeal(Collider2D ally)
        {
            var hp = ally.GetComponent<Health>() ?? ally.GetComponentInParent<Health>();
            if (hp == null || !hp.IsAlive) return;
            hp.Heal(damage);
            DamageNumber.Spawn(transform.position, damage, new Color(0.4f, 1f, 0.5f));
            ImpactBurst3D.Spawn(transform.position, new Color(0.35f, 1f, 0.55f), count: 8, speed: 2.8f, lifetime: 0.30f, size: 0.075f);
            StickAudio.PlayHeal(transform.position);
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
            ImpactBurst3D.Spawn(transform.position, new Color(1f, 0.45f, 0.10f), count: 22, speed: 5.5f, lifetime: 0.55f, size: 0.12f);
            StickAudio.PlayExplosion(transform.position);
            HitStop.Punch(0.055f, 0.075f);
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
