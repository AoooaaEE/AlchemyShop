using System.Collections.Generic;
using StickEvolve.Combat;
using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Сочный фидбек на любом юните с Health: hit-flash, squash-and-stretch,
    /// death burst + camera shake + low-HP красный outline.
    /// </summary>
    [DisallowMultipleComponent]
    public class Juice : MonoBehaviour
    {
        [Tooltip("Сила трясения камеры на смерти этого юнита (0..1).")]
        public float deathShakeTrauma = 0.18f;
        [Tooltip("Цвет вспышки на момент удара.")]
        public Color flashColor = Color.white;
        [Tooltip("Длительность вспышки.")]
        public float flashDuration = 0.07f;
        [Tooltip("На сколько SCALE подскакивает на удар (1.0 = без эффекта).")]
        public float squashScale = 1.18f;
        [Tooltip("Длительность squash-and-stretch.")]
        public float squashDuration = 0.13f;
        [Tooltip("HP-порог, ниже которого появляется красная обводка.")]
        [Range(0f, 1f)] public float outlineHpFraction = 0.3f;
        [Tooltip("Цвет частиц на смерти.")]
        public Color deathParticleColor = new Color(1f, 0.45f, 0.25f, 1f);
        [Tooltip("Размер взрыва частиц.")]
        public int deathParticleCount = 9;

        private Health _hp;
        private readonly List<SpriteRenderer> _renderers = new();
        private readonly List<Color> _baseColors = new();
        private readonly List<MeshRenderer> _meshRenderers = new();
        private readonly List<Color> _meshBaseColors = new();
        private MaterialPropertyBlock _meshFlashBlock;
        private GameObject _outlineGO;
        private SpriteRenderer _outlineSR;
        private float _flashTimer;
        private float _squashTimer;
        private Vector3 _baseScale;
        private bool _hasBaseScale;

        public static Juice Attach(GameObject target)
        {
            if (target == null) return null;
            var existing = target.GetComponent<Juice>();
            if (existing != null) return existing;
            return target.AddComponent<Juice>();
        }

        private void Awake()
        {
            _hp = GetComponent<Health>();
            if (_hp == null) { enabled = false; return; }
            _hp.OnDamaged += OnDamaged;
            _hp.OnDeath += OnDeath;
            _hp.OnHpChanged += OnHpChanged;
        }

        private void OnDestroy()
        {
            if (_hp == null) return;
            _hp.OnDamaged -= OnDamaged;
            _hp.OnDeath -= OnDeath;
            _hp.OnHpChanged -= OnHpChanged;
        }

        private void Start()
        {
            CaptureRenderers();
            _baseScale = transform.localScale;
            _hasBaseScale = true;
        }

        private void CaptureRenderers()
        {
            _renderers.Clear();
            _baseColors.Clear();
            _meshRenderers.Clear();
            _meshBaseColors.Clear();
            if (_meshFlashBlock == null) _meshFlashBlock = new MaterialPropertyBlock();

            var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] == null) continue;
                // Пропускаем тени/щиты, которые мы пометили — иначе вспышка съест их прозрачность.
                if (srs[i].name == "Shadow" || srs[i].name == "DropShadow" || srs[i].name == "Shield") continue;
                _renderers.Add(srs[i]);
                _baseColors.Add(srs[i].color);
            }

            var mrs = GetComponentsInChildren<MeshRenderer>(includeInactive: false);
            for (int i = 0; i < mrs.Length; i++)
            {
                var mr = mrs[i];
                if (mr == null || mr.sharedMaterial == null) continue;
                // Не флешим факелы/эмберы/руны, которые могут быть дочерними декором — только части юнита.
                if (mr.GetComponentInParent<TorchLight>() != null || mr.GetComponentInParent<EmberParticles>() != null) continue;
                Color baseColor = Color.white;
                if (mr.sharedMaterial.HasProperty("_BaseColor")) baseColor = mr.sharedMaterial.GetColor("_BaseColor");
                else if (mr.sharedMaterial.HasProperty("_Color")) baseColor = mr.sharedMaterial.GetColor("_Color");
                _meshRenderers.Add(mr);
                _meshBaseColors.Add(baseColor);
            }
        }

        private void OnDamaged(float amount, Vector3 worldPos)
        {
            _flashTimer = flashDuration;
            _squashTimer = squashDuration;
            // Если рендереры пересобрались/появились — пере-кэш.
            if (_renderers.Count == 0 && _meshRenderers.Count == 0) CaptureRenderers();

            Color spark = amount >= 10f ? new Color(1f, 0.35f, 0.10f) : new Color(0.75f, 0.90f, 1f);
            ImpactBurst3D.Spawn(worldPos, spark, count: 7, speed: 3.2f, lifetime: 0.28f, size: 0.075f);
            if (amount >= 6f) CameraShaker.Shake(0.025f);
        }

        private void OnDeath()
        {
            DeathBurst.Spawn(transform.position, deathParticleColor, deathParticleCount);
            ImpactBurst3D.SpawnDeath(transform.position, deathParticleColor);
            if (deathShakeTrauma > 0f) CameraShaker.Shake(deathShakeTrauma);
            if (_outlineGO != null) Destroy(_outlineGO);
            // Останавливаем сочки, чтобы не драться с DeathFallAnimator за scale/color.
            _flashTimer = 0f;
            _squashTimer = 0f;
            if (_hasBaseScale) transform.localScale = _baseScale;
            enabled = false;
        }

        private void OnHpChanged(float current, float max)
        {
            if (max <= 0f) return;
            float frac = current / max;
            bool wantOutline = current > 0f && frac <= outlineHpFraction;
            if (wantOutline) EnsureOutline();
            else if (_outlineGO != null) _outlineGO.SetActive(false);
        }

        private void EnsureOutline()
        {
            if (_outlineGO != null)
            {
                _outlineGO.SetActive(true);
                return;
            }
            if (_renderers.Count == 0) CaptureRenderers();
            if (_renderers.Count == 0) return;
            // Делаем второй sprite чуть больше и темно-красным под основным — простая «обводка-аура».
            var main = _renderers[0];
            _outlineGO = new GameObject("LowHpOutline");
            _outlineGO.transform.SetParent(main.transform, worldPositionStays: false);
            _outlineGO.transform.localPosition = Vector3.zero;
            _outlineGO.transform.localRotation = Quaternion.identity;
            _outlineGO.transform.localScale = Vector3.one * 1.18f;
            _outlineSR = _outlineGO.AddComponent<SpriteRenderer>();
            _outlineSR.sprite = main.sprite;
            _outlineSR.color = new Color(1f, 0.15f, 0.15f, 0.55f);
            _outlineSR.sortingOrder = main.sortingOrder - 1;
        }

        private void Update()
        {
            // Flash
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_flashTimer / flashDuration);
                for (int i = 0; i < _renderers.Count; i++)
                {
                    var r = _renderers[i];
                    if (r == null) continue;
                    r.color = Color.Lerp(_baseColors[i], flashColor, t);
                }
                for (int i = 0; i < _meshRenderers.Count; i++)
                {
                    var r = _meshRenderers[i];
                    if (r == null || r.sharedMaterial == null) continue;
                    r.GetPropertyBlock(_meshFlashBlock);
                    Color c = Color.Lerp(_meshBaseColors[i], flashColor, t);
                    if (r.sharedMaterial.HasProperty("_BaseColor")) _meshFlashBlock.SetColor("_BaseColor", c);
                    if (r.sharedMaterial.HasProperty("_Color")) _meshFlashBlock.SetColor("_Color", c);
                    r.SetPropertyBlock(_meshFlashBlock);
                }

                if (_flashTimer <= 0f)
                {
                    for (int i = 0; i < _renderers.Count; i++)
                        if (_renderers[i] != null) _renderers[i].color = _baseColors[i];
                    for (int i = 0; i < _meshRenderers.Count; i++)
                    {
                        var r = _meshRenderers[i];
                        if (r == null || r.sharedMaterial == null) continue;
                        r.GetPropertyBlock(_meshFlashBlock);
                        if (r.sharedMaterial.HasProperty("_BaseColor")) _meshFlashBlock.SetColor("_BaseColor", _meshBaseColors[i]);
                        if (r.sharedMaterial.HasProperty("_Color")) _meshFlashBlock.SetColor("_Color", _meshBaseColors[i]);
                        r.SetPropertyBlock(_meshFlashBlock);
                    }
                }
            }

            // Squash-and-stretch
            if (_squashTimer > 0f && _hasBaseScale)
            {
                _squashTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(_squashTimer / squashDuration);
                // Кривая: быстрый «пинок» на 0..0.35, затем плавно обратно.
                float k = t < 0.35f
                    ? Mathf.SmoothStep(0f, 1f, t / 0.35f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f);
                float sx = Mathf.Lerp(1f, squashScale, k);
                float sy = Mathf.Lerp(1f, 2f - squashScale, k * 0.6f); // лёгкая анти-фаза по Y
                transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, _baseScale.z);
                if (_squashTimer <= 0f) transform.localScale = _baseScale;
            }
        }
    }
}
