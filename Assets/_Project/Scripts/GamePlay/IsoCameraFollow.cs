using UnityEngine;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Изометрическая камера, плавно следующая за target.
    /// </summary>
    public class IsoCameraFollow : MonoBehaviour
    {
        [Header("Цель")]
        [SerializeField] private Transform target;

        [Header("Параметры")]
        [SerializeField] private Vector3 offset       = new Vector3(0f, 12f, -8f);
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private float   smoothTime   = 0.2f;

        private Vector3 velocity;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;

            var desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position + lookAtOffset);
        }
    }
}