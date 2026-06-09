using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Мерцающий факел: точечный свет + эмиссивный «огонёк» поверх. Используется на колоннах.
    /// Свет «дышит» через Perlin-шум, чтобы выглядел как живое пламя.
    /// </summary>
    [DisallowMultipleComponent]
    public class TorchLight : MonoBehaviour
    {
        [SerializeField] public Light pointLight;
        [SerializeField] public Renderer flameRenderer;
        [SerializeField] public float baseIntensity = 2.5f;
        [SerializeField] public float baseRange = 4.5f;
        [SerializeField] public Color flameColor = new Color(1.0f, 0.55f, 0.18f);

        private float _seed;
        private MaterialPropertyBlock _mpb;

        private void Awake()
        {
            _seed = Random.Range(0f, 100f);
            _mpb = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (pointLight == null) return;
            float t = Time.time * 6f + _seed;
            float n = Mathf.PerlinNoise(t, 0f);          // 0..1
            float flicker = 0.75f + n * 0.5f;             // 0.75..1.25

            pointLight.intensity = baseIntensity * flicker;
            pointLight.range = baseRange * (0.9f + n * 0.2f);
            pointLight.color = flameColor;

            if (flameRenderer != null)
            {
                flameRenderer.GetPropertyBlock(_mpb);
                Color emit = flameColor * (1.8f * flicker);
                emit.a = 1f;
                if (flameRenderer.sharedMaterial != null && flameRenderer.sharedMaterial.HasProperty("_EmissionColor"))
                    _mpb.SetColor("_EmissionColor", emit);
                if (flameRenderer.sharedMaterial != null && flameRenderer.sharedMaterial.HasProperty("_BaseColor"))
                    _mpb.SetColor("_BaseColor", flameColor * (1.2f + n * 0.3f));
                flameRenderer.SetPropertyBlock(_mpb);
            }
        }

        public static TorchLight Spawn(Transform parent, Vector3 localPos, Color color)
        {
            // Корень факела.
            var root = new GameObject("Torch");
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = localPos;

            // Точечный свет.
            var lightGO = new GameObject("FlameLight");
            lightGO.transform.SetParent(root.transform, worldPositionStays: false);
            lightGO.transform.localPosition = new Vector3(0f, 0f, 0.15f);
            var pl = lightGO.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = color;
            pl.intensity = 2.5f;
            pl.range = 4.5f;
            pl.shadows = LightShadows.None;

            // Эмиссивный «огонёк» (вытянутый кубик).
            var flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flame.name = "Flame";
            var col = flame.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            flame.transform.SetParent(root.transform, worldPositionStays: false);
            flame.transform.localPosition = new Vector3(0f, 0f, 0.20f);
            flame.transform.localScale = new Vector3(0.22f, 0.22f, 0.45f);
            var mr = flame.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LitMaterial.GetEmissive(color, 1.8f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Чаша факела (тёмная подставка).
            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bowl.name = "Bowl";
            var col2 = bowl.GetComponent<Collider>(); if (col2 != null) Object.Destroy(col2);
            bowl.transform.SetParent(root.transform, worldPositionStays: false);
            bowl.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            bowl.transform.localScale = new Vector3(0.32f, 0.32f, 0.10f);
            bowl.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial.Get(new Color(0.10f, 0.07f, 0.05f));

            var torch = root.AddComponent<TorchLight>();
            torch.pointLight = pl;
            torch.flameRenderer = mr;
            torch.flameColor = color;
            return torch;
        }
    }

    /// <summary>
    /// Парящие угольки/искры в воздухе для атмосферы. Дешёвые кубики, медленно поднимаются и зацикливаются.
    /// </summary>
    [DisallowMultipleComponent]
    public class EmberParticles : MonoBehaviour
    {
        private struct Ember
        {
            public Transform t;
            public Vector3 vel;
            public float life;
            public float maxLife;
            public Vector3 origin;
        }

        private Ember[] _embers;
        public Vector2 areaXY = new Vector2(10f, 14f);
        public float minZ = 0.2f;
        public float maxZ = 4.5f;
        public Color color = new Color(1f, 0.55f, 0.15f);
        public int count = 32;

        public static EmberParticles Spawn(Transform parent, Vector2 area, Color color, int count = 32)
        {
            var go = new GameObject("EmberParticles");
            go.transform.SetParent(parent, worldPositionStays: false);
            var ep = go.AddComponent<EmberParticles>();
            ep.areaXY = area;
            ep.color = color;
            ep.count = count;
            ep.Init();
            return ep;
        }

        private void Init()
        {
            _embers = new Ember[count];
            var mat = LitMaterial.GetEmissive(color, 1.2f);
            for (int i = 0; i < count; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Ember";
                var col = cube.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
                cube.transform.SetParent(transform, worldPositionStays: false);
                float s = Random.Range(0.04f, 0.08f);
                cube.transform.localScale = new Vector3(s, s, s);
                var mr = cube.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                _embers[i] = new Ember
                {
                    t = cube.transform,
                    origin = RandomPos(),
                    vel = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(0.4f, 0.9f)),
                    life = Random.Range(0f, 3f),
                    maxLife = Random.Range(3f, 5f),
                };
                _embers[i].t.localPosition = _embers[i].origin;
            }
        }

        private Vector3 RandomPos()
        {
            return new Vector3(
                Random.Range(-areaXY.x * 0.5f, areaXY.x * 0.5f),
                Random.Range(-areaXY.y * 0.5f, areaXY.y * 0.5f),
                Random.Range(minZ, minZ + 0.6f));
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _embers.Length; i++)
            {
                ref var e = ref _embers[i];
                e.life += dt;
                if (e.life >= e.maxLife)
                {
                    e.life = 0f;
                    e.origin = RandomPos();
                    e.t.localPosition = e.origin;
                    e.vel = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), Random.Range(0.4f, 0.9f));
                }
                else
                {
                    var p = e.t.localPosition + e.vel * dt;
                    if (p.z > maxZ) p.z = minZ;
                    e.t.localPosition = p;
                }
            }
        }
    }
}
