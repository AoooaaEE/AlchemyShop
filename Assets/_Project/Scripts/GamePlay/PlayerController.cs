using UnityEngine;
using UnityEngine.AI;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Игрок: движение через WASD (Editor) или экранный джойстик (Android).
    /// Использует NavMeshAgent для коллижна со столами и стенами.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Движение")]
        [SerializeField] private float moveSpeed     = 4f;
        [SerializeField] private float rotationSpeed = 12f;

        private NavMeshAgent agent;
        private Vector2      joystickInput;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.speed          = moveSpeed;
            agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.avoidancePriority    = 0; // максимальный приоритет, остальные уступают
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Передаётся виртуальным джойстиком (Шаг 13.3).</summary>
        public void SetJoystickInput(Vector2 dir)
        {
            joystickInput = Vector2.ClampMagnitude(dir, 1f);
        }

        private void Update()
        {
            // 1. WASD/стрелки в Editor.
            Vector2 keyInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));

            // 2. Если клавиатура активна — она перебивает джойстик.
            Vector2 finalInput = keyInput.sqrMagnitude > 0.01f ? keyInput : joystickInput;

            if (finalInput.sqrMagnitude < 0.01f)
            {
                agent.velocity = Vector3.zero;
                return;
            }

            // X = лево/право (мировая X), Y = вперёд/назад (мировая Z).
            Vector3 worldDir = new Vector3(finalInput.x, 0f, finalInput.y).normalized;

            // Двигаем через NavMeshAgent — он сам не пройдёт сквозь столы.
            agent.Move(worldDir * moveSpeed * Time.deltaTime);

            // Плавный поворот в направлении движения.
            Quaternion targetRot = Quaternion.LookRotation(worldDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                rotationSpeed * Time.deltaTime);
        }
    }
}