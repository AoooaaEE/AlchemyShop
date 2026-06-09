using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Code-only звук без ассетов: короткие процедурные клипы для hit/crit/death/heal/explosion.
    /// Это не финальный саунд-дизайн, но сразу убирает ощущение немой тестовой сцены.
    /// </summary>
    [DisallowMultipleComponent]
    public class StickAudio : MonoBehaviour
    {
        private static StickAudio _instance;

        private AudioSource _oneShots;
        private AudioSource _ambient;
        private AudioClip _hit;
        private AudioClip _heavyHit;
        private AudioClip _crit;
        private AudioClip _death;
        private AudioClip _heal;
        private AudioClip _explosion;
        private AudioClip _shoot;
        private AudioClip _ambientLoop;
        private float _nextTinyHitTime;

        public static StickAudio Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("StickAudio");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<StickAudio>();
            return _instance;
        }

        public static void PlayHit(float amount, Vector3 worldPos)
        {
            var a = Ensure();
            if (a == null) return;
            // Мелкие многократные удары слегка троттлим, чтобы не было аудио-каши.
            if (amount < 3f && Time.unscaledTime < a._nextTinyHitTime) return;
            if (amount < 3f) a._nextTinyHitTime = Time.unscaledTime + 0.035f;
            a.Play(amount >= 6f ? a._heavyHit : a._hit, amount >= 6f ? 0.45f : 0.30f, worldPos);
        }

        public static void PlayCrit(Vector3 worldPos)
        {
            var a = Ensure();
            if (a != null) a.Play(a._crit, 0.55f, worldPos);
        }

        public static void PlayDeath(Vector3 worldPos)
        {
            var a = Ensure();
            if (a != null) a.Play(a._death, 0.45f, worldPos);
        }

        public static void PlayHeal(Vector3 worldPos)
        {
            var a = Ensure();
            if (a != null) a.Play(a._heal, 0.35f, worldPos);
        }

        public static void PlayExplosion(Vector3 worldPos)
        {
            var a = Ensure();
            if (a != null) a.Play(a._explosion, 0.55f, worldPos);
        }

        public static void PlayShoot(Vector3 worldPos)
        {
            var a = Ensure();
            if (a != null) a.Play(a._shoot, 0.16f, worldPos);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0.20f;
            _oneShots.rolloffMode = AudioRolloffMode.Linear;
            _oneShots.maxDistance = 18f;
            _oneShots.volume = 0.85f;

            _ambient = gameObject.AddComponent<AudioSource>();
            _ambient.playOnAwake = false;
            _ambient.loop = true;
            _ambient.spatialBlend = 0f;
            _ambient.volume = 0.09f;

            BuildClips();
            StartAmbient();
        }

        private void BuildClips()
        {
            _hit = Tone("hit", 92f, 52f, 0.055f, Wave.NoiseClick);
            _heavyHit = Tone("heavy_hit", 78f, 36f, 0.080f, Wave.NoiseClick);
            _crit = Tone("crit", 520f, 860f, 0.115f, Wave.Triangle);
            _death = Tone("death", 140f, 42f, 0.220f, Wave.SawNoise);
            _heal = Tone("heal", 360f, 680f, 0.160f, Wave.Sine);
            _explosion = Tone("explosion", 110f, 28f, 0.260f, Wave.NoiseClick);
            _shoot = Tone("shoot", 680f, 330f, 0.045f, Wave.Triangle);
            _ambientLoop = Ambient("arena_ambient", 3.0f);
        }

        private void StartAmbient()
        {
            if (_ambientLoop == null || _ambient == null) return;
            _ambient.clip = _ambientLoop;
            if (!_ambient.isPlaying) _ambient.Play();
        }

        private void Play(AudioClip clip, float volume, Vector3 worldPos)
        {
            if (clip == null || _oneShots == null) return;
            var cam = Camera.main;
            transform.position = cam != null ? cam.transform.position : worldPos;
            _oneShots.pitch = Random.Range(0.94f, 1.06f);
            _oneShots.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private enum Wave { Sine, Triangle, SawNoise, NoiseClick }

        private static AudioClip Tone(string name, float f0, float f1, float seconds, Wave wave)
        {
            const int rate = 44100;
            int count = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
            var data = new float[count];
            float phase = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float freq = Mathf.Lerp(f0, f1, t);
                phase += freq / rate;
                phase -= Mathf.Floor(phase);
                float env = Mathf.Exp(-7.5f * t) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.03f));
                float sample;
                switch (wave)
                {
                    case Wave.Triangle:
                        sample = 4f * Mathf.Abs(phase - 0.5f) - 1f;
                        break;
                    case Wave.SawNoise:
                        sample = (phase * 2f - 1f) * 0.55f + Random.Range(-0.45f, 0.45f) * (1f - t);
                        break;
                    case Wave.NoiseClick:
                        sample = Mathf.Sin(phase * Mathf.PI * 2f) * 0.35f + Random.Range(-1f, 1f) * 0.65f * (1f - t);
                        break;
                    default:
                        sample = Mathf.Sin(phase * Mathf.PI * 2f);
                        break;
                }
                data[i] = Mathf.Clamp(sample * env, -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Ambient(string name, float seconds)
        {
            const int rate = 44100;
            int count = Mathf.Max(1, Mathf.RoundToInt(rate * seconds));
            var data = new float[count];
            float p1 = 0f, p2 = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                p1 += 58f / rate;
                p2 += 86f / rate;
                p1 -= Mathf.Floor(p1);
                p2 -= Mathf.Floor(p2);
                float loopFade = Mathf.Sin(t * Mathf.PI); // мягкий loop, без клика на краях
                float hum = Mathf.Sin(p1 * Mathf.PI * 2f) * 0.33f + Mathf.Sin(p2 * Mathf.PI * 2f) * 0.18f;
                float noise = Random.Range(-0.035f, 0.035f);
                data[i] = (hum + noise) * Mathf.Lerp(0.7f, 1f, loopFade);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
