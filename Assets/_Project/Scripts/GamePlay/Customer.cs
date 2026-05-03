using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Alchemy.Data;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Клиент: идёт к слоту в очереди → ждёт → после обслуживания уходит к выходу и удаляется.
    /// Держит рецепт, который хочет купить, и показывает иконку-каплю над головой.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        public enum State { Approaching, Waiting, Leaving }

        [SerializeField] private float arriveDist = 0.3f;
        [SerializeField] private Vector3 iconOffset = new Vector3(0f, 1.35f, 0f);

        private NavMeshAgent agent;
        private State        state;
        private Vector3      exitPosition;
        private RecipeSO     recipe;
        private Image        iconImage;

        public State    Current    => state;
        public bool     IsWaiting  => state == State.Waiting;
        public RecipeSO Recipe     => recipe;

        public void SetExit(Vector3 worldPos) => exitPosition = worldPos;

        public void SetRecipe(RecipeSO r)
        {
            recipe = r;
            if (iconImage == null) CreateIcon();
            if (iconImage != null && recipe != null)
                iconImage.color = recipe.iconColor;
        }

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

        private void CreateIcon()
        {
            var root = new GameObject("RecipeIcon");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = iconOffset;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<CanvasScaler>();

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(80f, 110f);
            rt.localScale = Vector3.one * 0.005f;

            // Белая подложка для читаемости на любом фоне.
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(-10f, -10f);
            bgRt.offsetMax = new Vector2(10f, 10f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = UpgradePad.GetDropletSprite();
            bgImg.color  = new Color(1f, 1f, 1f, 0.85f);
            bgImg.preserveAspect = true;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(root.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            iconImage = iconGo.AddComponent<Image>();
            iconImage.sprite = UpgradePad.GetDropletSprite();
            iconImage.color  = Color.magenta;
            iconImage.preserveAspect = true;

            root.AddComponent<BillboardToCamera>();
        }
    }
}
