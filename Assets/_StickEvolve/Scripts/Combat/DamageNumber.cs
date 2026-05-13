using TMPro;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Всплывающая циферка урона. Сама себя удаляет через lifetime секунд.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.7f;
        [SerializeField] private Vector3 driftPerSec = new Vector3(0f, 1.2f, 0f);

        private float _age;
        private TextMeshPro _text;
        private Color _startColor;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _startColor = _text.color;
        }

        public void Show(float amount, Color color)
        {
            _text.text = Mathf.RoundToInt(amount).ToString();
            _text.color = color;
            _startColor = color;
            _age = 0f;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += driftPerSec * Time.deltaTime;
            float t = _age / lifetime;
            var c = _startColor;
            c.a = Mathf.Clamp01(1f - t);
            _text.color = c;
            if (_age >= lifetime) Destroy(gameObject);
        }

        public static DamageNumber Spawn(Vector3 worldPos, float amount, Color color)
        {
            var go = new GameObject("DamageNumber");
            go.transform.position = worldPos + new Vector3(0f, 0.3f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 100;
            var dn = go.AddComponent<DamageNumber>();
            dn.Show(amount, color);
            return dn;
        }
    }
}
