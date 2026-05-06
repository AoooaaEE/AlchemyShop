using UnityEngine;
using UnityEngine.AI;
using Alchemy.Data;
using Alchemy.Utils;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Раз в spawnInterval секунд спавнит клиента, если в очереди есть место.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {        public static CustomerSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        [Header("Спавн")]
        [SerializeField] private float   spawnInterval = 3.5f;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1f, 9f);
        [SerializeField] private Vector3 exitPosition  = new Vector3(0f, 1f, 12f);

        [Header("Параметры NavMeshAgent клиента")]
        [SerializeField] private float agentRadius = 0.45f;
        [SerializeField] private float agentHeight = 1.7f;
        [SerializeField] private float agentSpeed  = 2.5f;

        private float timer;
        private bool  spawningEnabled = true;

        public void Configure(Vector3 spawn, Vector3 exit, float interval)
        {
            spawnPosition = spawn;
            exitPosition  = exit;
            spawnInterval = interval;
            timer         = spawnInterval * 0.4f; // первый клиент быстрее
        }

        /// <summary>
        /// Управляет, спавнить ли клиентов сейчас. До открытия лавки клиентов нет.
        /// </summary>
        public void SetSpawningEnabled(bool enabled)
        {
            spawningEnabled = enabled;
            if (enabled) timer = Mathf.Min(timer, 1f); // первый сразу после открытия
        }

        private void Update()
        {
            if (!spawningEnabled) return;
            if (CustomerQueue.Instance == null) return;
            if (!CustomerQueue.Instance.HasFreeSlot) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = spawnInterval;
                Spawn();
            }
        }

        // Намеренно НЕ используем Mage (это игрок-алхимик) и RogueHooded (это подмастерье),
        // чтобы клиенты не сливались с персоналом.
        private static readonly string[] CustomerModels =
            { "Knight", "Barbarian", "Rogue" };

        private void Spawn()
        {
            // 1. Пытаемся взять одну из KayKit-моделей, откатываемся на капсулу.
            string modelName = CustomerModels[Random.Range(0, CustomerModels.Length)];
            var go = ModelLoader.TryInstantiateCharacter(modelName, transform);
            bool usedModel = (go != null);
            if (!usedModel)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                ApplyRandomColor(go);
            }
            go.name = "Customer";
            go.transform.position = spawnPosition;
            ModelLoader.StripColliders(go);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius              = agentRadius;
            agent.height              = agentHeight;
            agent.speed               = agentSpeed;
            agent.angularSpeed        = 360f;
            agent.acceleration        = 12f;
            agent.stoppingDistance    = 0.4f;          // не въезжают в стол/в спину соседу
            agent.autoBraking         = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            // Модель KayKit имеет pivot у ступней → baseOffset 0 (агент стоит
            // ровно на NavMesh). Капсула — pivot в центре, ей нужен offset.
            agent.baseOffset   = usedModel ? 0f : 0.7f;

            if (NavMesh.SamplePosition(spawnPosition, out var hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            var customer = go.AddComponent<Customer>();
            customer.SetExit(exitPosition);
            // Иконка над головой: модели KayKit ~1.8м, ставим капельку повыше.
            customer.SetIconOffsetY(usedModel ? 2.1f : 1.35f);

            var recipe = RecipeBook.Random();
            if (recipe != null)
            {
                customer.SetRecipe(recipe);
                // С моделью KayKit оригинальные текстуры важнее тонировки —
                // цвет рецепта показывает только иконка над головой.
                if (!usedModel)
                    TintBody(go, recipe.iconColor);
            }

            CustomerQueue.Instance.Enqueue(customer);
        }

        private static void TintBody(GameObject go, Color color)
        {
            var rend = go.GetComponent<MeshRenderer>();
            if (rend == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            var body = Color.Lerp(rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.gray, color, 0.55f);
            mat.color = body;
            rend.sharedMaterial = mat;
        }

        private static void ApplyRandomColor(GameObject go)
        {
            var rend = go.GetComponent<MeshRenderer>();
            if (rend == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = Random.ColorHSV(0f, 1f, 0.4f, 0.7f, 0.7f, 1f);
            rend.sharedMaterial = mat;
        }
    
        public void SetInterval(float newInterval)
        {
            spawnInterval = Mathf.Max(0.1f, newInterval);
        }
    }
}