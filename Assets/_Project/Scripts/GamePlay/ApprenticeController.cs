using Alchemy.Core;
using Alchemy.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Подмастерье: автономно ходит Shelf → Cauldron → BottlingTable
    /// и продаёт зелья без участия клиентов. Пассивный доход.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ApprenticeController : MonoBehaviour
    {
        public enum State { Idle, MoveTo, Working }

        [Header("Поведение")]
        [SerializeField, Min(0.1f)]            private float arriveDist  = 0.4f;
        [SerializeField, Range(0.1f, 1.5f)]    private float speedFactor = 0.7f;
        [SerializeField, Min(0.1f)]            private float idleRetryInterval = 1f;

        private static readonly WorkstationType[] Cycle =
        {
            WorkstationType.Shelf,
            WorkstationType.Cauldron,
            WorkstationType.BottlingTable
        };

        private NavMeshAgent agent;
        private RecipeSO     recipe;

        private State state;
        private float workTimer;
        private float idleTimer;
        private int   cycleIndex;
        private bool  ready;

        private void Awake()
        {
            agent  = GetComponent<NavMeshAgent>();
            recipe = RecipeBook.Default;
            if (recipe == null)
                Debug.LogError("[Apprentice] Не найден базовый рецепт в Resources/Recipes.");
        }

                private void Start()
        {
            agent.speed             *= speedFactor;
            agent.avoidancePriority  = 60;       // дефолт 50, алхимик 50, подмастерье уступает
            agent.radius             = 0.3f;     // более тонкий
            cycleIndex = 0;
            EnterMove();
            ready = true;
        }
        private void Update()
        {
            if (!ready) return;

            switch (state)
            {
                case State.Idle:
                    // Стоим до тех пор, пока все 3 станции не построены (Shelf/Cauldron/Table).
                    idleTimer -= Time.deltaTime;
                    if (idleTimer <= 0f)
                    {
                        idleTimer = idleRetryInterval;
                        if (HasAllStations()) EnterMove();
                    }
                    break;

                case State.MoveTo:
                    if (!agent.pathPending && agent.remainingDistance <= arriveDist)
                        EnterWorking();
                    break;

                case State.Working:
                    workTimer -= Time.deltaTime;
                    if (workTimer <= 0f) FinishWorking();
                    break;
            }
        }

        private static bool HasAllStations()
        {
            return Workstation.Find(WorkstationType.Shelf)         != null
                && Workstation.Find(WorkstationType.Cauldron)      != null
                && Workstation.Find(WorkstationType.BottlingTable) != null;
        }

        private void EnterIdle()
        {
            state     = State.Idle;
            idleTimer = idleRetryInterval;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        }

                private void EnterMove()
        {
            var ws = Workstation.Find(Cycle[cycleIndex]);
            if (ws == null) { EnterIdle(); return; }

            // Сдвиг точки на 0.8м в сторону, чтобы не толкаться с алхимиком.
            Vector3 target = ws.InteractionPosition + new Vector3(0.8f, 0f, 0f);

            if (NavMesh.SamplePosition(target, out var hit, 5f, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                state = State.MoveTo;
                agent.SetDestination(hit.position);
            }
            else
            {
                EnterIdle();
            }
        }
        private void EnterWorking()
        {
            state = State.Working;
            if (agent.isOnNavMesh) agent.ResetPath();

            workTimer = Cycle[cycleIndex] switch
            {
                WorkstationType.Shelf         => recipe != null ? recipe.pickupTime : 0.6f,
                WorkstationType.Cauldron      => recipe != null ? recipe.brewTime   : 3f,
                WorkstationType.BottlingTable => recipe != null ? recipe.bottleTime : 0.6f,
                _ => 1f
            };
        }

        private void FinishWorking()
        {
            if (Cycle[cycleIndex] == WorkstationType.BottlingTable)
                SellPotion();

            cycleIndex = (cycleIndex + 1) % Cycle.Length;

            // На старте нового цикла (Shelf) подмастерье выбирает случайный рецепт.
            if (cycleIndex == 0)
            {
                var next = RecipeBook.Random();
                if (next != null) recipe = next;
            }

            EnterMove();
        }

        private void SellPotion()
        {
            if (recipe == null) return;

            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy == null) return;

            long price = economy.GetSellPrice(recipe.basePrice);
            economy.AddGold(price);
            Debug.Log($"[Apprentice] Сварил «{recipe.displayName}» за {price}. Всего золота: {economy.Gold}");
        }
    }
}