using UnityEngine;

namespace StickEvolve.Combat
{
    /// <summary>
    /// Простая «походка» стикмена: ноги/руки качаются sin-волной, когда корень двигается.
    /// Если враг/герой стоит — _moveBlend плавно гаснет, всё возвращается в позу покоя.
    /// </summary>
    public class StickmanAnimator : MonoBehaviour
    {
        public Transform legL;
        public Transform legR;
        public Transform armL;
        public Transform armR;
        public Transform torso;
        public Transform head;

        [Tooltip("Если true — правая рука зафиксирована в позе стрельбы (как у героя).")]
        public bool keepRightArmRaised;

        public float walkFreq = 7f;
        public float legSwingDeg = 28f;
        public float armSwingDeg = 18f;
        public float bobAmp = 0.04f;
        public float blendSpeed = 6f;

        private Vector3 _lastPos;
        private float _phase;
        private float _moveBlend;
        private float _torsoBaseY;

        public void SetTorsoBaseY(float y) => _torsoBaseY = y;

        private void Awake()
        {
            _lastPos = transform.position;
            if (torso != null) _torsoBaseY = torso.localPosition.y;
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

            float legAng = Mathf.Sin(_phase) * legSwingDeg * _moveBlend;
            float armAng = Mathf.Sin(_phase) * armSwingDeg * _moveBlend;

            if (legL != null) legL.localRotation = Quaternion.Euler(0f, 0f, legAng);
            if (legR != null) legR.localRotation = Quaternion.Euler(0f, 0f, -legAng);

            if (armL != null) armL.localRotation = Quaternion.Euler(0f, 0f, -armAng);
            if (armR != null && !keepRightArmRaised)
                armR.localRotation = Quaternion.Euler(0f, 0f, armAng);

            if (torso != null)
            {
                var lp = torso.localPosition;
                lp.y = _torsoBaseY + Mathf.Abs(Mathf.Sin(_phase * 2f)) * bobAmp * _moveBlend;
                torso.localPosition = lp;
            }
        }
    }
}
