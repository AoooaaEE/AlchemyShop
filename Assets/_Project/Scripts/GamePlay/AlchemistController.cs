using Alchemy.Core;
using Alchemy.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// FSM алхимика. Ждёт клиента → Shelf → Cauldron → BottlingTable (продажа + клиент уходит) → ждёт следующего.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AlchemistController : MonoBehaviour
    {
        public enum State { Idle, MoveTo, Working }

        [Header("Поведение")]
        [SerializeField] private float arriveDist = 0.25f;

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
        private int   cycleIndex;

        public State Current => state;

        private void Awake()
        {
            agent  = GetComponent<NavMeshAgent>();
            recipe = RecipeBook.Default;
            if (recipe == null)
                Debug.LogError("[Alchemist] Не найден базовый рецепт в Resources/Recipes.");
        }

        private void Start()
        {
            state = State.Idle;
        }

        private void Update()
        {
            switch (state)
            {
                case State.Idle:
                    if (CustomerQueue.Instance != null && CustomerQueue.Instance.FrontIsReady())
                    {
                        cycleIndex = 0;
                        EnterMove();
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

        private void EnterMove()
        {
            var ws = Workstation.Find(Cycle[cycleIndex]);
            if (ws == null) { state = State.Idle; return; }

            if (NavMesh.SamplePosition(ws.InteractionPosition, out var hit, 5f, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                state = State.MoveTo;
                agent.SetDestination(hit.position);
            }
            else
            {
                state = State.Idle;
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
            var current = Cycle[cycleIndex];

            if (current == WorkstationType.BottlingTable)
            {
                ServeCurrentCustomer();
            }

            cycleIndex = (cycleIndex + 1) % Cycle.Length;

            if (cycleIndex == 0)
            {
                // Цикл закончен — ждём следующего клиента.
                state = State.Idle;
            }
            else
            {
                EnterMove();
            }
        }

        private void ServeCurrentCustomer()
        {
            if (recipe == null) return;

            CustomerQueue.Instance?.ServeFront();

            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy != null)
            {
                long price = economy.GetSellPrice(recipe.basePrice);
                economy.AddGold(price);
                Debug.Log($"[Alchemist] Продал «{recipe.displayName}» за {price}. Всего золота: {economy.Gold}");
            }
        }
    }
}