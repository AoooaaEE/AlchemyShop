using UnityEngine;

namespace StickEvolve.VFX
{
    /// <summary>
    /// Простая 3D-анимация для процедурных персонажей: ходьба, idle breathing,
    /// поворот силуэта в сторону движения/атаки и короткий weapon swing.
    /// Всё кодом, без Animator Controller и без ассетов.
    /// </summary>
    [DisallowMultipleComponent]
    public class Character3DAnimator : MonoBehaviour
    {
        [Header("Motion")]
        public float walkFreq = 8f;
        public float legSwingDeg = 18f;
        public float armSwingDeg = 14f;
        public float bobAmp = 0.055f;
        public float blendSpeed = 8f;
        public float faceTurnSpeed = 540f;

        [Header("Attack")]
        public float attackRecoverSpeed = 7f;
        public float weaponSwingDeg = 55f;

        private Transform _visual;
        private Transform _legL;
        private Transform _legR;
        private Transform _armL;
        private Transform _armR;
        private Transform _torso;
        private Transform _head;
        private Transform _cape;
        private Transform _weapon;
        private Transform _groundGlow;

        private Vector3 _lastWorldPos;
        private Vector3 _visualBaseLocalPos;
        private Vector3 _torsoBaseLocalPos;
        private Vector3 _headBaseLocalPos;
        private Vector3 _capeBaseLocalPos;
        private Vector3 _groundGlowBaseScale;
        private Quaternion _visualTargetRot = Quaternion.identity;
        private float _phase;
        private float _moveBlend;
        private float _attackKick;
        private bool _hasFaceDir;

        private void Awake()
        {
            _visual = transform;
            _legL = FindDeep("LegL");
            _legR = FindDeep("LegR");
            _armL = FindDeep("ArmL");
            _armR = FindDeep("ArmR");
            _torso = FindDeep("Torso");
            _head = FindDeep("Head");
            _cape = FindDeep("Cape");
            _weapon = FindDeep("Weapon");
            _groundGlow = FindDeep("GroundGlow");

            _lastWorldPos = transform.position;
            _visualBaseLocalPos = _visual.localPosition;
            if (_torso != null) _torsoBaseLocalPos = _torso.localPosition;
            if (_head != null) _headBaseLocalPos = _head.localPosition;
            if (_cape != null) _capeBaseLocalPos = _cape.localPosition;
            if (_groundGlow != null) _groundGlowBaseScale = _groundGlow.localScale;
        }

        public void TriggerAttack(Vector3 targetWorldPos)
        {
            FaceWorldPoint(targetWorldPos);
            _attackKick = 1f;
        }

        public void FaceWorldPoint(Vector3 targetWorldPos)
        {
            Vector3 dir = targetWorldPos - transform.position;
            SetLookDirection(new Vector2(dir.x, dir.y));
        }

        public void SetLookDirection(Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            // У модели "лицо" смотрит по локальной -Y. Поворачиваем вокруг локального Z.
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            _visualTargetRot = Quaternion.Euler(0f, 0f, angle);
            _hasFaceDir = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 worldPos = transform.position;
            Vector3 delta = worldPos - _lastWorldPos;
            _lastWorldPos = worldPos;

            float planarSpeed = new Vector2(delta.x, delta.y).magnitude / dt;
            float targetBlend = planarSpeed > 0.04f ? 1f : 0f;
            _moveBlend = Mathf.MoveTowards(_moveBlend, targetBlend, dt * blendSpeed);
            _phase += dt * walkFreq * Mathf.Lerp(0.35f, 1f, _moveBlend);

            if (!_hasFaceDir && planarSpeed > 0.05f)
                SetLookDirection(new Vector2(delta.x, delta.y));

            if (_hasFaceDir)
                _visual.localRotation = Quaternion.RotateTowards(_visual.localRotation, _visualTargetRot, faceTurnSpeed * dt);

            _attackKick = Mathf.MoveTowards(_attackKick, 0f, dt * attackRecoverSpeed);

            float walk = Mathf.Sin(_phase);
            float legAng = walk * legSwingDeg * _moveBlend;
            float armAng = walk * armSwingDeg * _moveBlend;
            float attack = Mathf.Sin(_attackKick * Mathf.PI);

            if (_legL != null) _legL.localRotation = Quaternion.Euler(legAng, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(-legAng, 0f, 0f);
            if (_armL != null) _armL.localRotation = Quaternion.Euler(-armAng * 0.75f, 0f, 0f);
            if (_armR != null) _armR.localRotation = Quaternion.Euler(armAng + attack * -weaponSwingDeg, 0f, attack * 12f);
            if (_weapon != null) _weapon.localRotation = Quaternion.Euler(attack * -weaponSwingDeg, 0f, attack * 18f);

            float bob = Mathf.Abs(Mathf.Sin(_phase * 2f)) * bobAmp * _moveBlend;
            float idle = Mathf.Sin(Time.time * 1.7f) * 0.018f * (1f - _moveBlend);
            _visual.localPosition = _visualBaseLocalPos + new Vector3(0f, 0f, bob + idle + attack * 0.025f);

            if (_torso != null)
                _torso.localPosition = _torsoBaseLocalPos + new Vector3(0f, -attack * 0.035f, attack * 0.035f);
            if (_head != null)
                _head.localPosition = _headBaseLocalPos + new Vector3(0f, -attack * 0.025f, idle * 0.45f);
            if (_cape != null)
                _cape.localPosition = _capeBaseLocalPos + new Vector3(0f, Mathf.Sin(_phase) * 0.025f * _moveBlend, 0f);
            if (_groundGlow != null && _groundGlowBaseScale.sqrMagnitude > 0.0001f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 3.1f) * 0.025f + attack * 0.08f;
                _groundGlow.localScale = _groundGlowBaseScale * pulse;
            }
        }

        private Transform FindDeep(string childName)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == childName) return all[i];
            }
            return null;
        }
    }
}
