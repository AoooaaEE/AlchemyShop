using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Анимация стикмена:
    /// • Ходьба (sin-волна ног/рук + bob торса) — активна, когда корень движется.
    /// • Idle-дыхание — мягкое слабое колебание торса/головы в покое.
    /// • Eye blink — периодическое моргание (alpha = 0 на короткое время).
    /// • TriggerShoot — кратковременный наклон головы и отдача правой руки.
    /// </summary>
    public class StickmanAnimator : MonoBehaviour
    {
        public Transform legL;
        public Transform legR;
        public Transform armL;
        public Transform armR;
        public Transform torso;
        public Transform head;
        public Transform eyeL;
        public Transform eyeR;

        [Tooltip("Если true — правая рука зафиксирована в позе стрельбы (как у героя).")]
        public bool keepRightArmRaised;

        public float walkFreq = 7f;
        public float legSwingDeg = 28f;
        public float armSwingDeg = 18f;
        public float bobAmp = 0.04f;
        public float blendSpeed = 6f;

        [Tooltip("Базовый угол поднятой правой руки (если keepRightArmRaised).")]
        public float raisedArmAngle = 75f;

        // Idle-дыхание
        public float idleBreathFreq = 1.6f;
        public float idleBreathAmp = 0.012f;

        // Моргание
        public float blinkIntervalMin = 2.5f;
        public float blinkIntervalMax = 5.5f;
        public float blinkDuration = 0.10f;

        private Vector3 _lastPos;
        private float _phase;
        private float _moveBlend;
        private float _torsoBaseY;
        private float _headBaseY;
        private float _shootKick;          // 0..1, импульс отдачи руки
        private float _shootHeadKick;      // 0..1, импульс наклона головы
        private float _blinkUntil = -1f;
        private float _nextBlinkTime;
        private SpriteRenderer _eyeLSr;
        private SpriteRenderer _eyeRSr;

        public void SetTorsoBaseY(float y) => _torsoBaseY = y;
        public void SetHeadBaseY(float y) => _headBaseY = y;

        /// <summary>Триггер анимации отдачи. Поднятая рука дёргается назад, голова чуть наклоняется.</summary>
        public void TriggerShoot()
        {
            _shootKick = 1f;
            _shootHeadKick = 1f;
        }

        private void Awake()
        {
            _lastPos = transform.position;
            if (torso != null) _torsoBaseY = torso.localPosition.y;
            if (head != null) _headBaseY = head.localPosition.y;
            if (eyeL != null) _eyeLSr = eyeL.GetComponent<SpriteRenderer>();
            if (eyeR != null) _eyeRSr = eyeR.GetComponent<SpriteRenderer>();
            _nextBlinkTime = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float vel = (transform.position - _lastPos).magnitude / dt;
            _lastPos = transform.position;

            float target = vel > 0.1f ? 1f : 0f;
            _moveBlend = Mathf.MoveTowards(_moveBlend, target, dt * blendSpeed);

            _phase += dt * walkFreq * _moveBlend;

            // — Конечности —
            float legAng = Mathf.Sin(_phase) * legSwingDeg * _moveBlend;
            float armAng = Mathf.Sin(_phase) * armSwingDeg * _moveBlend;

            if (legL != null) legL.localRotation = Quaternion.Euler(0f, 0f, legAng);
            if (legR != null) legR.localRotation = Quaternion.Euler(0f, 0f, -legAng);

            if (armL != null) armL.localRotation = Quaternion.Euler(0f, 0f, -armAng);
            if (armR != null && !keepRightArmRaised)
                armR.localRotation = Quaternion.Euler(0f, 0f, armAng);
            else if (armR != null && keepRightArmRaised)
            {
                _shootKick = Mathf.MoveTowards(_shootKick, 0f, dt * 6f);
                float kickOffset = _shootKick * 30f;
                armR.localRotation = Quaternion.Euler(0f, 0f, raisedArmAngle + kickOffset);
            }

            // — Торс: bob при ходьбе + idle-дыхание в покое —
            if (torso != null)
            {
                float lp_y = _torsoBaseY;
                lp_y += Mathf.Abs(Mathf.Sin(_phase * 2f)) * bobAmp * _moveBlend;
                lp_y += Mathf.Sin(Time.time * idleBreathFreq) * idleBreathAmp * (1f - _moveBlend);
                var p = torso.localPosition; p.y = lp_y;
                torso.localPosition = p;
            }

            // — Голова: лёгкий bob + отдача при выстреле —
            if (head != null)
            {
                _shootHeadKick = Mathf.MoveTowards(_shootHeadKick, 0f, dt * 5f);
                float headTilt = _shootHeadKick * -8f;
                head.localRotation = Quaternion.Euler(0f, 0f, headTilt);

                float hy = _headBaseY;
                hy += Mathf.Sin(Time.time * idleBreathFreq + 0.4f) * idleBreathAmp * 0.6f * (1f - _moveBlend);
                var hp = head.localPosition; hp.y = hy;
                head.localPosition = hp;
            }

            // — Eye blink —
            if (Time.time >= _nextBlinkTime && _blinkUntil < 0f)
            {
                _blinkUntil = Time.time + blinkDuration;
                _nextBlinkTime = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
            }
            if (_blinkUntil > 0f)
            {
                bool isClosed = Time.time < _blinkUntil;
                SetEyeAlpha(isClosed ? 0f : 1f);
                if (!isClosed) _blinkUntil = -1f;
            }
        }

        private void SetEyeAlpha(float a)
        {
            if (_eyeLSr != null) { var c = _eyeLSr.color; c.a = a; _eyeLSr.color = c; }
            if (_eyeRSr != null) { var c = _eyeRSr.color; c.a = a; _eyeRSr.color = c; }
        }
    }
}
