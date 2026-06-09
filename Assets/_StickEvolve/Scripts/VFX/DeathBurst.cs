using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Короткий «взрыв» из круглых частиц при смерти. Без ParticleSystem ассета — просто
    /// несколько GameObject со SpriteRenderer'ом и линейной симуляцией. Дёшево и сочно.
    /// </summary>
    public class DeathBurst : MonoBehaviour
    {
        public float lifetime = 0.55f;
        private float _age;
        private Vector3 _vel;
        private SpriteRenderer _sr;
        private Color _start;
        private float _scaleStart;

        public static void Spawn(Vector3 worldPos, Color color, int count = 8, float speed = 4.5f, float size = 0.18f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var go = new GameObject("DeathBurst");
                go.transform.position = worldPos;
                go.transform.localScale = Vector3.one * size * Random.Range(0.85f, 1.25f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ParticleSprite.Get();
                sr.color = color;
                sr.sortingOrder = 90;
                var p = go.AddComponent<DeathBurst>();
                p._sr = sr;
                p._start = color;
                p._scaleStart = go.transform.localScale.x;
                p._vel = dir * speed * Random.Range(0.6f, 1.2f);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / lifetime);
            transform.position += _vel * Time.deltaTime;
            // gravity-ish drag и оседание вниз для большего «веса»
            _vel *= 1f - 2.5f * Time.deltaTime;
            _vel.y -= 6f * Time.deltaTime;

            if (_sr != null)
            {
                var c = _start; c.a = 1f - t; _sr.color = c;
                float s = _scaleStart * (1f - 0.4f * t);
                transform.localScale = new Vector3(s, s, 1f);
            }
            if (_age >= lifetime) Destroy(gameObject);
        }
    }

    /// <summary>
    /// Ленивый источник круглого спрайта-частички — генерим в Texture2D один раз.
    /// Изолирован, чтобы не тащить зависимость на SpriteFactory.
    /// </summary>
    internal static class ParticleSprite
    {
        private static Sprite _cached;
        public static Sprite Get()
        {
            if (_cached != null) return _cached;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                float a = Mathf.Clamp01(1f - d);
                a = a * a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _cached = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _cached.name = "ParticleCircle";
            return _cached;
        }
    }
}
