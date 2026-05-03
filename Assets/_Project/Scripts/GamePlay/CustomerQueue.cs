using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Gameplay
{
    /// <summary>
    /// Очередь клиентов: ставит в строй, обслуживает переднего, остальные смещаются вперёд.
    /// Один экземпляр в сцене — синглтон.
    /// </summary>
    public class CustomerQueue : MonoBehaviour
    {
        public static CustomerQueue Instance { get; private set; }

        [Header("Слоты очереди")]
        [SerializeField] private Vector3 frontSlotWorld = new Vector3(0f, 0f, 2f);
        [SerializeField] private float   slotSpacing    = 1.2f;
        [SerializeField] private int     maxSize        = 4;

        private readonly List<Customer> queue = new();

        public int  Count       => queue.Count;
        public bool HasFreeSlot => queue.Count < maxSize;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Enqueue(Customer c)
        {
            if (c == null || queue.Contains(c)) return;
            queue.Add(c);
            c.MoveToSlot(GetSlotPosition(queue.Count - 1));
        }

        public Customer PeekFront() => queue.Count > 0 ? queue[0] : null;

        public bool FrontIsReady() => queue.Count > 0 && queue[0] != null && queue[0].IsWaiting;

        public void ServeFront()
        {
            if (queue.Count == 0) return;

            var c = queue[0];
            queue.RemoveAt(0);
            if (c != null) c.LeaveServed();

            // Сдвигаем остальных вперёд.
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i] != null)
                    queue[i].MoveToSlot(GetSlotPosition(i));
            }
        }

        public Vector3 GetSlotPosition(int index)
        {
            return frontSlotWorld + Vector3.forward * (slotSpacing * index);
        }

        public void Configure(Vector3 frontSlot, float spacing, int max)
        {
            frontSlotWorld = frontSlot;
            slotSpacing    = spacing;
            maxSize        = max;
        }
    
        public void SetMaxSize(int newMax)
        {
            maxSize = Mathf.Max(1, newMax);
        }}
}