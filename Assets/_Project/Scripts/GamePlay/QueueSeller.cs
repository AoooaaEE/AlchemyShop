using UnityEngine;
using UnityEngine.UI;
using Alchemy.Core;
using Alchemy.Data;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Прилавок у переднего слота очереди. Игрок с BottledPotion + фронт ждёт →
    /// короткий таймер → продажа: +золото, клиент уходит, кубик пропадает.
    /// </summary>
    public class QueueSeller : MonoBehaviour
    {
        [SerializeField] private float sellTime          = 0.4f;
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private Vector3 barOffset       = new Vector3(0f, 1.5f, 0f);

        private float      timer;
        private RecipeSO   fallbackRecipe;
        private GameObject barRoot;
        private Image      barFill;

        private void Awake()
        {
            fallbackRecipe = RecipeBook.Default;
        }

        private void Start()
        {
            CreateProgressBar();
        }

        private void Update()
        {
            var player = PlayerController.Instance;
            if (player == null) { ResetTimer(); return; }

            var carry = player.GetComponent<PlayerCarry>();
            if (carry == null || carry.Item != CarryItem.BottledPotion) { ResetTimer(); return; }

            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > interactionRadius) { ResetTimer(); return; }

            if (CustomerQueue.Instance == null || !CustomerQueue.Instance.FrontIsReady())
            { ResetTimer(); return; }

            timer += Time.deltaTime;
            UpdateBar(timer / sellTime);

            if (timer >= sellTime)
            {
                Sell(carry);
                ResetTimer();
            }
        }

        private void Sell(PlayerCarry carry)
        {
            // Рецепт переднего клиента определяет цену продажи.
            var front = CustomerQueue.Instance.PeekFront();
            var r = (front != null && front.Recipe != null) ? front.Recipe : fallbackRecipe;

            CustomerQueue.Instance.ServeFront();

            var economy = GameManager.Instance != null ? GameManager.Instance.Economy : null;
            if (economy != null && r != null)
            {
                long price = economy.GetSellPrice(r.basePrice);
                economy.AddGold(price);
                Debug.Log($"[Player] Продал «{r.displayName}» за {price}. Всего золота: {economy.Gold}");
            }

            carry.SetItem(CarryItem.None);
        }

        private void ResetTimer()
        {
            timer = 0f;
            UpdateBar(0f);
        }

        private void UpdateBar(float fill)
        {
            if (barRoot == null) return;
            barRoot.SetActive(fill > 0.001f);
            if (barFill != null) barFill.fillAmount = Mathf.Clamp01(fill);
        }

        private void CreateProgressBar()
        {
            barRoot = new GameObject("ProgressBar");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = barOffset;

            var canvas = barRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            barRoot.AddComponent<CanvasScaler>();

            var rt = barRoot.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(1.2f, 0.18f);
            rt.localScale = Vector3.one;

            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(barRoot.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = WorkstationProcessor.GetWhiteSprite();
            bgImg.color  = new Color(0f, 0f, 0f, 0.6f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barRoot.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(0.02f, 0.02f);
            fillRt.offsetMax = new Vector2(-0.02f, -0.02f);

            barFill = fillGo.AddComponent<Image>();
            barFill.sprite     = WorkstationProcessor.GetWhiteSprite();
            barFill.color      = new Color(0.95f, 0.85f, 0.3f); // золотистый
            barFill.type       = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 0f;

            barRoot.AddComponent<BillboardToCamera>();
            barRoot.SetActive(false);
        }
    }
}