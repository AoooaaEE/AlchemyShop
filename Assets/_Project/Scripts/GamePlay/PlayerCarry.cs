using System;
using UnityEngine;

namespace Alchemy.Gameplay
{
    public enum CarryItem
    {
        None,
        Ingredient,
        BrewedPotion,
        BottledPotion
    }

    /// <summary>
    /// Что игрок несёт. Над головой — цветной кубик.
    /// </summary>
    public class PlayerCarry : MonoBehaviour
    {
        public CarryItem Item { get; private set; } = CarryItem.None;
        public event Action<CarryItem> OnItemChanged;

        [SerializeField] private float visualHeight = 1.4f;
        [SerializeField] private float visualSize   = 0.3f;

        private GameObject   visual;
        private MeshRenderer visualRend;

        private void Awake()
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "CarryVisual";

            // Убираем коллайдер чтоб не мешал движению/NavMesh.
            var col = visual.GetComponent<Collider>();
            if (col != null) Destroy(col);

            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, visualHeight, 0f);
            visual.transform.localScale    = Vector3.one * visualSize;
            visual.SetActive(false);

            visualRend = visual.GetComponent<MeshRenderer>();
        }

        public void SetItem(CarryItem item)
        {
            if (Item == item) return;
            Item = item;
            UpdateVisual();
            OnItemChanged?.Invoke(item);
        }

        private void UpdateVisual()
        {
            if (visual == null) return;

            if (Item == CarryItem.None)
            {
                visual.SetActive(false);
                return;
            }

            visual.SetActive(true);

            Color color = Item switch
            {
                CarryItem.Ingredient    => new Color(0.4f, 0.8f, 0.3f),
                CarryItem.BrewedPotion  => new Color(0.9f, 0.3f, 0.3f),
                CarryItem.BottledPotion => new Color(0.3f, 0.5f, 0.9f),
                _                       => Color.white
            };

            if (visualRend != null) visualRend.material.color = color;
        }
    }
}