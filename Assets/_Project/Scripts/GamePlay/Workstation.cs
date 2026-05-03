using System.Collections.Generic;
using UnityEngine;

namespace Alchemy.Gameplay
{
    public enum WorkstationType { Shelf, Cauldron, BottlingTable }

    /// <summary>
    /// База для всех рабочих мест. Регистрируется в статическом списке,
    /// чтобы алхимик мог найти нужный стол по типу.
    /// </summary>
    public class Workstation : MonoBehaviour
    {
        public static List<Workstation> All { get; } = new();

        [SerializeField] private WorkstationType type;
        [SerializeField] private Transform       interactionPoint;

        public WorkstationType Type => type;

        public Vector3 InteractionPosition =>
            interactionPoint != null ? interactionPoint.position : transform.position;

        public void Setup(WorkstationType t, Transform interaction)
        {
            type = t;
            interactionPoint = interaction;
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public static Workstation Find(WorkstationType t)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].Type == t) return All[i];
            return null;
        }
    }
}