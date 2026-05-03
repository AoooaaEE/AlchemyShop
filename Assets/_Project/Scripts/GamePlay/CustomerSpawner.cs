using UnityEngine;
using UnityEngine.AI;

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
        [SerializeField] private float agentRadius = 0.3f;
        [SerializeField] private float agentHeight = 1.5f;
        [SerializeField] private float agentSpeed  = 2.5f;

        private float timer;

        public void Configure(Vector3 spawn, Vector3 exit, float interval)
        {
            spawnPosition = spawn;
            exitPosition  = exit;
            spawnInterval = interval;
            timer         = spawnInterval * 0.4f; // первый клиент быстрее
        }

        private void Update()
        {
            if (CustomerQueue.Instance == null) return;
            if (!CustomerQueue.Instance.HasFreeSlot) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = spawnInterval;
                Spawn();
            }
        }

        private void Spawn()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Customer";
            go.transform.SetParent(transform, false);
            go.transform.position   = spawnPosition;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            ApplyRandomColor(go);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius       = agentRadius;
            agent.height       = agentHeight;
            agent.speed        = agentSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.baseOffset   = 0.7f; // капсула scale 0.7 → половина высоты ≈ 0.7

            if (NavMesh.SamplePosition(spawnPosition, out var hit, 5f, NavMesh.AllAreas))
                agent.Warp(hit.position);

            var customer = go.AddComponent<Customer>();
            customer.SetExit(exitPosition);

            CustomerQueue.Instance.Enqueue(customer);
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