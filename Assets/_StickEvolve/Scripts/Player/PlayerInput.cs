using UnityEngine;

namespace StickEvolve.Player
{
    /// <summary>
    /// Универсальный ввод движения для топ-даун арены:
    ///   • в редакторе/на ПК — WASD и стрелки.
    ///   • на тач-устройстве — виртуальный джойстик из любой точки левой половины экрана:
    ///     первое касание = центр джойстика, дальнейший дрэг = направление + сила.
    /// </summary>
    public static class PlayerInput
    {
        private static bool _touchActive;
        private static int _touchFingerId = -1;
        private static Vector2 _touchOrigin;
        private const float TouchMaxRadiusPx = 140f;

        /// <summary>Текущее направление движения (величина 0..1).</summary>
        public static Vector2 Movement
        {
            get
            {
                Vector2 kb = ReadKeyboard();
                Vector2 tj = ReadTouchJoystick();
                // Тач имеет приоритет, если активен
                if (tj.sqrMagnitude > 0.0001f) return tj;
                return kb;
            }
        }

        private static Vector2 ReadKeyboard()
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        private static Vector2 ReadTouchJoystick()
        {
            if (Input.touchCount == 0)
            {
                _touchActive = false;
                _touchFingerId = -1;
                return Vector2.zero;
            }

            // Если уже отслеживаем палец — найдём его в текущих тач-данных.
            if (_touchActive && _touchFingerId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.fingerId != _touchFingerId) continue;
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        _touchActive = false;
                        _touchFingerId = -1;
                        return Vector2.zero;
                    }
                    Vector2 delta = t.position - _touchOrigin;
                    float mag = delta.magnitude;
                    if (mag < 8f) return Vector2.zero;
                    float normMag = Mathf.Min(mag / TouchMaxRadiusPx, 1f);
                    return delta.normalized * normMag;
                }
                // Палец потерян
                _touchActive = false;
                _touchFingerId = -1;
            }

            // Ищем новый палец в левой половине экрана.
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Began) continue;
                if (t.position.x > Screen.width * 0.5f) continue;
                _touchActive = true;
                _touchFingerId = t.fingerId;
                _touchOrigin = t.position;
                return Vector2.zero;
            }
            return Vector2.zero;
        }
    }
}
