using UnityEngine;
using UnityEngine.EventSystems;

namespace Alchemy.UI
{
    /// <summary>
    /// Экранный джойстик: пользователь тащит «ручку» по фоновому кругу,
    /// получаем нормализованный Vector2 в диапазоне [-1..1].
    /// Каждый кадр пушим Input в PlayerController.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float          radius = 100f;

        public Vector2 Input    { get; private set; }
        public bool    IsActive { get; private set; }

               public void Setup(RectTransform bg, RectTransform k, float radius)
        {
            background   = bg;
            knob         = k;
            this.radius  = radius;
        }

        public void OnPointerDown(PointerEventData ev)
        {
            IsActive = true;
            UpdateKnob(ev);
        }

        public void OnDrag(PointerEventData ev)
        {
            UpdateKnob(ev);
        }

        public void OnPointerUp(PointerEventData ev)
        {
            IsActive = false;
            Input    = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
        }

        private void UpdateKnob(PointerEventData ev)
        {
            if (background == null || knob == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, ev.position, ev.pressEventCamera, out var local);

            local = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = local;

            Input = local / radius;
        }

        private void Update()
        {
            // Передаём ввод игроку каждый кадр.
            if (Alchemy.Gameplay.PlayerController.Instance != null)
                Alchemy.Gameplay.PlayerController.Instance.SetJoystickInput(Input);
        }
    }
}