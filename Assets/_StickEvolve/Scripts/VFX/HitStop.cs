using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Короткая заморозка времени на сильных попаданиях. Это дешёвый, но очень заметный
    /// game-feel приём: удар ощущается тяжелее, а крит/смерть читаются лучше.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitStop : MonoBehaviour
    {
        private static HitStop _instance;

        private float _restoreScale = 1f;
        private float _timer;
        private bool _active;

        public static HitStop Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("HitStop");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<HitStop>();
            return _instance;
        }

        /// <summary>
        /// duration: реальные секунды (unscaled). slowScale 0.04..0.35 — насколько замедлить игру.
        /// </summary>
        public static void Punch(float duration = 0.045f, float slowScale = 0.08f)
        {
            var hs = Ensure();
            if (hs == null) return;
            hs.StartPunch(duration, slowScale);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void StartPunch(float duration, float slowScale)
        {
            duration = Mathf.Clamp(duration, 0.01f, 0.12f);
            slowScale = Mathf.Clamp(slowScale, 0.02f, 0.35f);

            if (!_active)
            {
                _restoreScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            }

            _active = true;
            _timer = Mathf.Max(_timer, duration);
            Time.timeScale = Mathf.Min(Time.timeScale <= 0f ? 1f : Time.timeScale, slowScale);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }

        private void Update()
        {
            if (!_active) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;

            Time.timeScale = Mathf.Approximately(_restoreScale, 0f) ? 1f : _restoreScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _timer = 0f;
            _active = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Time.timeScale = Mathf.Approximately(_restoreScale, 0f) ? 1f : _restoreScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                _instance = null;
            }
        }
    }
}
