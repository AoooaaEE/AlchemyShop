using TMPro;
using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Всплывающая циферка урона. В 3D-режиме ведёт себя как billboard:
    /// всегда смотрит в камеру, чуть подпрыгивает и держится над персонажем по Z.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumber : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.82f;
        [SerializeField] private Vector3 driftPerSec = new Vector3(0f, 0.65f, 0.55f);
        [SerializeField] private float popScale = 1.25f;

        private float _age;
        private TextMeshPro _text;
        private Color _startColor;
        private Vector3 _baseScale;
        private Camera _cam;
        private bool _emphasis;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _startColor = _text.color;
            _baseScale = transform.localScale;
            _cam = Camera.main;
        }

        public void Show(float amount, Color color, bool emphasis = false)
        {
            _emphasis = emphasis;
            _text.text = emphasis ? $"{Mathf.RoundToInt(amount)}!" : Mathf.RoundToInt(amount).ToString();
            _text.color = color;
            _startColor = color;
            _age = 0f;
            _baseScale = transform.localScale * (emphasis ? 1.18f : 1f);
            transform.localScale = _baseScale * popScale;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;
            transform.position += driftPerSec * dt;

            float t = Mathf.Clamp01(_age / lifetime);
            var c = _startColor;
            c.a = Mathf.Clamp01(1f - t);
            _text.color = c;

            float pop = Mathf.Lerp(popScale, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.22f)));
            float emphasisPulse = _emphasis ? 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.035f * (1f - t) : 1f;
            transform.localScale = _baseScale * pop * emphasisPulse;

            if (_age >= lifetime) Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            // Billboard для перспективной камеры: текст развёрнут к камере и остаётся читаемым над 3D-юнитами.
            Vector3 toCamera = transform.position - _cam.transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        public static DamageNumber Spawn(Vector3 worldPos, float amount, Color color, bool emphasis = false)
        {
            var go = new GameObject(emphasis ? "DamageNumber_Crit" : "DamageNumber");
            // Z-подъём нужен для 3D-персонажей; в 2D он почти не влияет на сортировку.
            go.transform.position = worldPos + new Vector3(0f, 0.22f, 1.15f);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.fontSize = emphasis ? 5.2f : 4.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 100;
            tmp.enableWordWrapping = false;
            tmp.outlineWidth = emphasis ? 0.22f : 0.16f;
            tmp.outlineColor = new Color(0.02f, 0.01f, 0.03f, 0.95f);
            var dn = go.AddComponent<DamageNumber>();
            dn.Show(amount, color, emphasis);
            return dn;
        }
    }
}
