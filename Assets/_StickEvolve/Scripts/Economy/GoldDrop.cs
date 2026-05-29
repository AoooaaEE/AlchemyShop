using StickEvolve.Combat;
using StickEvolve.Core;
using UnityEngine;

namespace StickEvolve.Economy
{
    /// <summary>
    /// Падающая визуальная монета. В прототипе золото уже начислено в StickEconomy при смерти врага,
    /// монетка — только визуальный фидбек. Тапать НЕ нужно. Сама исчезает через lifetime.
    /// </summary>
    public class GoldDrop : MonoBehaviour
    {
        public float lifetime = 1.2f;
        public Vector3 drift = new Vector3(0f, 1.5f, 0f);
        private float _age;
        private SpriteRenderer _sr;
        private Color _start;

        public static GoldDrop Spawn(Vector3 pos, int amount)
        {
            var go = new GameObject($"GoldDrop_{amount}");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Circle(32);
            sr.color = ColorPalette.GoldDrop;
            sr.sortingOrder = 6;
            go.transform.localScale = Vector3.one * 0.25f;
            var gd = go.AddComponent<GoldDrop>();
            gd._sr = sr;
            gd._start = sr.color;
            return gd;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += drift * Time.deltaTime;
            float t = _age / lifetime;
            var c = _start;
            c.a = Mathf.Clamp01(1f - t);
            _sr.color = c;
            if (_age >= lifetime) Destroy(gameObject);
        }
    }
}
