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
        public enum State { MoveTo, Working }

        [Header("Поведение")]
        [SerializeField, Min(0.1f)]            private float arriveDist  = 0.4f;
        [SerializeField, Range(0.1f, 1.5f)]    private float speedFactor = 0.7f;

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
        private bool  ready;

        private void Awake()
        {
            agent  = GetComponent<NavMeshAgent>();
            recipe = Resources.Load<RecipeSO>("Recipes/HealingPotion");
            if (recipe == null)
                Debug.LogError("[Apprentice] Не найден рецепт Resources/Recipes/HealingPotion.");
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
            if (ws == null) return;

            // Сдвиг точки на 0.8м в сторону, чтобы не толкаться с алхимиком.
            Vector3 target = ws.InteractionPosition + new Vector3(0.8f, 0f, 0f);

            if (NavMesh.SamplePosition(target, out var hit, 5f, NavMesh.AllAreas)
                && agent.isOnNavMesh)
            {
                state = State.MoveTo;
                agent.SetDestination(hit.position);
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