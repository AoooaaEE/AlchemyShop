using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Когда стикмен умирает — этот компонент берёт на себя анимацию падения:
    /// заваливается на 90°, ужимается, гаснет, потом удаляется. Параллельно
    /// блокируется Update Hero/Enemy через флаг IsAlive (т.к. CurrentHp=0).
    /// </summary>
    public class DeathFallAnimator : MonoBehaviour
    {
        public float duration = 0.6f;
        public float fallAngleDeg = 90f;
        public float shrinkTo = 0.7f;

        private float _age;
        private Quaternion _startRot;
        private Vector3 _startScale;
        private SpriteRenderer[] _renderers;
        private float[] _startAlphas;

        public static void Begin(GameObject target)
        {
            if (target == null) return;
            if (target.GetComponent<DeathFallAnimator>() != null) return;
            // Колайдер и rigidbody отключаем, чтобы пули/тригеры не дёргали труп.
            var col = target.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            target.AddComponent<DeathFallAnimator>();
        }

        private void Awake()
        {
            _startRot = transform.rotation;
            _startScale = transform.localScale;
            _renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: false);
            _startAlphas = new float[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _startAlphas[i] = _renderers[i].color.a;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / duration);
            // Кривая «удара о землю»: 80% поворота за первые 40% времени.
            float tRot = 1f - Mathf.Pow(1f - t, 3f);
            float angle = Mathf.Lerp(0f, fallAngleDeg, tRot);
            transform.rotation = _startRot * Quaternion.Euler(0f, 0f, angle);

            float scaleMul = Mathf.Lerp(1f, shrinkTo, t);
            transform.localScale = _startScale * scaleMul;

            float a = Mathf.Lerp(1f, 0f, t * t);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                var c = _renderers[i].color;
                c.a = _startAlphas[i] * a;
                _renderers[i].color = c;
            }

            if (_age >= duration) Destroy(gameObject);
        }
    }
}
