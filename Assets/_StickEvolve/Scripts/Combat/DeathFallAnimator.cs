using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Когда персонаж умирает — этот компонент берёт на себя анимацию падения:
    /// 2D-стикмен заваливается по Z, 3D-меш — по X, ужимается и исчезает.
    /// Параллельно блокируется Update Hero/Enemy через флаг IsAlive (CurrentHp=0).
    /// </summary>
    public class DeathFallAnimator : MonoBehaviour
    {
        public float duration = 0.6f;
        public float fallAngleDeg = 90f;
        public float shrinkTo = 0.7f;

        private float _age;
        private Quaternion _startRot;
        private Vector3 _startScale;
        private SpriteRenderer[] _spriteRenderers;
        private float[] _spriteStartAlphas;
        private MeshRenderer[] _meshRenderers;
        private Material[] _meshMaterials;
        private Color[] _meshStartColors;
        private bool _has3DMesh;

        public static void Begin(GameObject target)
        {
            if (target == null) return;
            if (target.GetComponent<DeathFallAnimator>() != null) return;
            // Коллайдер и rigidbody отключаем, чтобы пули/тригеры не дёргали труп.
            var col = target.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            target.AddComponent<DeathFallAnimator>();
        }

        private void Awake()
        {
            _startRot = transform.rotation;
            _startScale = transform.localScale;

            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(false);
            _spriteStartAlphas = new float[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
                _spriteStartAlphas[i] = _spriteRenderers[i].color.a;

            _meshRenderers = GetComponentsInChildren<MeshRenderer>(false);
            _has3DMesh = _meshRenderers.Length > 0;
            _meshMaterials = new Material[_meshRenderers.Length];
            _meshStartColors = new Color[_meshRenderers.Length];
            for (int i = 0; i < _meshRenderers.Length; i++)
            {
                if (_meshRenderers[i] == null) continue;
                _meshMaterials[i] = _meshRenderers[i].material;
                _meshStartColors[i] = ReadMaterialColor(_meshMaterials[i]);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / duration);
            // Кривая «удара о землю»: 80% поворота за первые 40% времени.
            float tRot = 1f - Mathf.Pow(1f - t, 3f);
            float angle = Mathf.Lerp(0f, fallAngleDeg, tRot);
            transform.rotation = _has3DMesh
                ? _startRot * Quaternion.Euler(angle, 0f, angle * 0.10f)
                : _startRot * Quaternion.Euler(0f, 0f, angle);

            float scaleMul = Mathf.Lerp(1f, shrinkTo, t);
            transform.localScale = _startScale * scaleMul;

            float a = Mathf.Lerp(1f, 0f, t * t);
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] == null) continue;
                var c = _spriteRenderers[i].color;
                c.a = _spriteStartAlphas[i] * a;
                _spriteRenderers[i].color = c;
            }

            // Даже если материал opaque и альфа не видна, к концу труп отключится;
            // на transparent/emissive деталях это даёт аккуратное затухание.
            for (int i = 0; i < _meshMaterials.Length; i++)
            {
                var mat = _meshMaterials[i];
                if (mat == null) continue;
                var c = _meshStartColors[i];
                c.a *= a;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c * Mathf.Lerp(0.45f, 0f, t));
            }

            if (_age >= duration) Destroy(gameObject);
        }

        private static Color ReadMaterialColor(Material mat)
        {
            if (mat == null) return Color.white;
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
            return Color.white;
        }
    }
}
