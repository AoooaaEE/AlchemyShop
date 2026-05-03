using UnityEngine;
using UnityEngine.AI;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Клиент: идёт к слоту в очереди → ждёт → после обслуживания уходит к выходу и удаляется.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        public enum State { Approaching, Waiting, Leaving }

        [SerializeField] private float arriveDist = 0.3f;

        private NavMeshAgent agent;
        private State        state;
        private Vector3      exitPosition;

        public State Current => state;
        public bool  IsWaiting => state == State.Waiting;

        public void SetExit(Vector3 worldPos) => exitPosition = worldPos;

        private void Awake() => agent = GetComponent<NavMeshAgent>();

        public void MoveToSlot(Vector3 worldPos)
        {
            if (state == State.Leaving) return;
            state = State.Approaching;
            if (NavMesh.SamplePosition(worldPos, out var hit, 5f, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                agent.SetDestination(hit.position);
            }
        }

        public void LeaveServed()
        {
            state = State.Leaving;
            if (NavMesh.SamplePosition(exitPosition, out var hit, 5f, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                agent.SetDestination(hit.position);
            }
        }

        private void Update()
        {
            if (agent.pathPending) return;

            if (agent.remainingDistance > arriveDist) return;

            switch (state)
            {
                case State.Approaching:
                    state = State.Waiting;
                    break;

                case State.Leaving:
                    Destroy(gameObject);
                    break;
            }
        }
    }
}