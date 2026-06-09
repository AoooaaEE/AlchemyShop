using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Code-only 3D hit/death burst: маленькие эмиссивные кубики/осколки, которые летят вверх,
    /// тухнут и уменьшаются. Нужен именно для 3D-режима, потому что старые SpriteRenderer-частицы
    /// с перспективной камерой выглядели плоско и «дешево».
    /// </summary>
    public class ImpactBurst3D : MonoBehaviour
    {
        private struct Shard
        {
            public Transform t;
            public MeshRenderer r;
            public Vector3 velocity;
            public Vector3 spin;
            public float startScale;
        }

        private Shard[] _shards;
        private Color _color;
        private float _age;
        private float _lifetime;
        private MaterialPropertyBlock _mpb;

        public static void Spawn(Vector3 worldPos, Color color, int count = 10, float speed = 3.8f, float lifetime = 0.35f, float size = 0.10f)
        {
            var root = new GameObject("ImpactBurst3D");
            root.transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z + 0.35f);
            var burst = root.AddComponent<ImpactBurst3D>();
            burst.Init(color, count, speed, lifetime, size);
        }

        public static void SpawnDeath(Vector3 worldPos, Color color)
        {
            Spawn(worldPos + new Vector3(0f, 0f, 0.25f), color, count: 18, speed: 5.2f, lifetime: 0.65f, size: 0.14f);
        }

        private void Init(Color color, int count, float speed, float lifetime, float size)
        {
            _color = color;
            _lifetime = Mathf.Max(0.05f, lifetime);
            _mpb = new MaterialPropertyBlock();
            _shards = new Shard[Mathf.Max(1, count)];
            var mat = LitMaterial.GetEmissive(color, 2.0f);

            for (int i = 0; i < _shards.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Shard";
                var col = cube.GetComponent<Collider>();
                if (col != null) Destroy(col);
                cube.transform.SetParent(transform, worldPositionStays: false);
                float s = size * Random.Range(0.65f, 1.35f);
                cube.transform.localScale = new Vector3(s, s, s);
                cube.transform.localPosition = Vector3.zero;
                cube.transform.localRotation = Random.rotation;

                var mr = cube.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float planar = speed * Random.Range(0.45f, 1.0f);
                Vector3 dir = new Vector3(Mathf.Cos(angle) * planar, Mathf.Sin(angle) * planar, speed * Random.Range(0.35f, 0.9f));
                _shards[i] = new Shard
                {
                    t = cube.transform,
                    r = mr,
                    velocity = dir,
                    spin = new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f), Random.Range(-360f, 360f)),
                    startScale = s,
                };
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _lifetime);
            float alpha = 1f - t;
            Color emit = _color * Mathf.Lerp(2.4f, 0.0f, t);
            emit.a = alpha;

            for (int i = 0; i < _shards.Length; i++)
            {
                var shard = _shards[i];
                if (shard.t == null) continue;

                shard.velocity *= 1f - 3.2f * Time.deltaTime;
                shard.velocity.z -= 5.5f * Time.deltaTime;
                shard.t.localPosition += shard.velocity * Time.deltaTime;
                shard.t.Rotate(shard.spin * Time.deltaTime, Space.Self);

                float s = shard.startScale * Mathf.Lerp(1.15f, 0.15f, t);
                shard.t.localScale = new Vector3(s, s, s);

                if (shard.r != null && shard.r.sharedMaterial != null)
                {
                    shard.r.GetPropertyBlock(_mpb);
                    if (shard.r.sharedMaterial.HasProperty("_EmissionColor")) _mpb.SetColor("_EmissionColor", emit);
                    if (shard.r.sharedMaterial.HasProperty("_BaseColor")) _mpb.SetColor("_BaseColor", new Color(_color.r, _color.g, _color.b, alpha));
                    shard.r.SetPropertyBlock(_mpb);
                }

                _shards[i] = shard;
            }

            if (_age >= _lifetime) Destroy(gameObject);
        }
    }
}
