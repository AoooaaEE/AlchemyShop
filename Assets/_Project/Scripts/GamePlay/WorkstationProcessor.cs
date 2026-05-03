using UnityEngine;
using UnityEngine.UI;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// На каждом столе. Если игрок рядом и несёт нужный input —
    /// постепенно превращает input в output. Над столом — прогресс-бар.
    /// </summary>
    public class WorkstationProcessor : MonoBehaviour
    {
        [SerializeField] private CarryItem inputItem;
        [SerializeField] private CarryItem outputItem;
        [SerializeField] private float    processTime       = 1.2f;
        [SerializeField] private float    interactionRadius = 2.2f;
        [SerializeField] private Vector3  barOffset         = new Vector3(0f, 1.5f, 0f);

        private float       timer;
        private GameObject  barRoot;
        private Image       barFill;

        public void Configure(CarryItem input, CarryItem output, float time)
        {
            inputItem   = input;
            outputItem  = output;
            processTime = time;
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
            if (carry == null) { ResetTimer(); return; }

            float dist = Vector3.Distance(player.transform.position, transform.position);
            if (dist > interactionRadius) { ResetTimer(); return; }

            if (carry.Item != inputItem) { ResetTimer(); return; }

            timer += Time.deltaTime;
            UpdateBar(timer / processTime);

            if (timer >= processTime)
            {
                carry.SetItem(outputItem);
                ResetTimer();
            }
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
                private static Sprite cachedWhite;
        public static Sprite GetWhiteSprite()
        {
            if (cachedWhite != null) return cachedWhite;
            var tex = new Texture2D(2, 2);
            var px  = new Color[] { Color.white, Color.white, Color.white, Color.white };
            tex.SetPixels(px);
            tex.Apply();
            cachedWhite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            return cachedWhite;
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

            // Фон
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(barRoot.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
                        var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = GetWhiteSprite();
            bgImg.color  = new Color(0f, 0f, 0f, 0.6f);

            // Заливка
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(barRoot.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(0.02f, 0.02f);
            fillRt.offsetMax = new Vector2(-0.02f, -0.02f);

                        barFill = fillGo.AddComponent<Image>();
            barFill.sprite     = GetWhiteSprite();
            barFill.color      = new Color(0.4f, 0.85f, 0.4f);
            barFill.type       = Image.Type.Filled;
            barFill.fillMethod = Image.FillMethod.Horizontal;
            barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFill.fillAmount = 0f;

            barRoot.AddComponent<BillboardToCamera>();
            barRoot.SetActive(false);
        }
    }

    /// <summary>Поворачивает объект лицом к main camera каждый кадр.</summary>
    public class BillboardToCamera : MonoBehaviour
    {
        private Camera cam;

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}