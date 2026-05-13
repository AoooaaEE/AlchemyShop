using UnityEngine;

namespace StickEvolve.Bootstrap
{
    /// <summary>
    /// Простое движение объекта влево с заворачиванием по X. Для облаков и фоновых элементов.
    /// </summary>
    public class ParallaxDrift : MonoBehaviour
    {
        public float speed = 0.1f;
        public float wrapX = -12f;
        public float resetX = 12f;

        private void Update()
        {
            var p = transform.position;
            p.x -= speed * Time.deltaTime;
            if (p.x < wrapX) p.x = resetX;
            transform.position = p;
        }
    }
}
