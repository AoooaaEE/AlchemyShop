using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Сидит на главной камере, умеет трясти её Perlin-шумом.
    /// Используется через статический Shake() — авто-находит/создаёт инстанс.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShaker : MonoBehaviour
    {
        private static CameraShaker _instance;

        private Vector3 _origin;
        private bool _originCaptured;
        private float _trauma;       // 0..1
        private float _decayPerSec = 1.5f;
        private float _maxOffset = 0.35f;
        private float _maxAngleDeg = 4f;
        private float _noiseSeedX, _noiseSeedY, _noiseSeedR;
        private const float NoiseSpeed = 28f;

        public static CameraShaker GetOrCreate()
        {
            if (_instance != null) return _instance;
            var cam = Camera.main;
            if (cam == null) return null;
            _instance = cam.gameObject.GetComponent<CameraShaker>();
            if (_instance == null) _instance = cam.gameObject.AddComponent<CameraShaker>();
            return _instance;
        }

        /// <summary>
        /// Добавить «травмы» камере. amount 0..1: 0.2 = лёгкий хит, 0.5 = смерть героя, 0.8 = босс.
        /// Можно вызывать многократно — травмы складываются (с ограничением 1).
        /// </summary>
        public static void Shake(float amount)
        {
            var s = GetOrCreate();
            if (s == null) return;
            s._trauma = Mathf.Clamp01(s._trauma + Mathf.Max(0f, amount));
        }

        private void Awake()
        {
            _instance = this;
            _noiseSeedX = Random.Range(0f, 1000f);
            _noiseSeedY = Random.Range(0f, 1000f);
            _noiseSeedR = Random.Range(0f, 1000f);
        }

        private void LateUpdate()
        {
            if (!_originCaptured)
            {
                _origin = transform.localPosition;
                _originCaptured = true;
            }

            if (_trauma <= 0f)
            {
                // Не трогаем localPosition каждый кадр когда нет шейка — но если кто-то двигает камеру кодом, обновим origin.
                _origin = transform.localPosition;
                transform.localRotation = Quaternion.identity;
                return;
            }

            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * NoiseSpeed;
            float ox = (Mathf.PerlinNoise(_noiseSeedX, t) * 2f - 1f) * _maxOffset * shake;
            float oy = (Mathf.PerlinNoise(_noiseSeedY, t) * 2f - 1f) * _maxOffset * shake;
            float oa = (Mathf.PerlinNoise(_noiseSeedR, t) * 2f - 1f) * _maxAngleDeg * shake;
            transform.localPosition = _origin + new Vector3(ox, oy, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, oa);

            _trauma = Mathf.Max(0f, _trauma - _decayPerSec * Time.unscaledDeltaTime);
            if (_trauma <= 0f)
            {
                transform.localPosition = _origin;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}
