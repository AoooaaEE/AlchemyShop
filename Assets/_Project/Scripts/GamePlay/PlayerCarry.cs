using System;
using UnityEngine;
using Alchemy.Utils;

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

        [SerializeField] private float visualHeight = 2.1f;
        [SerializeField] private float visualSize   = 0.3f;

        private GameObject visual;
        private bool       isModel;

        private void Awake()
        {
            // Сначала пробуем красивую 3D-бутылочку из KayKit; если её нет — куб-плейсхолдер.
            visual = ModelLoader.TryInstantiateProp("PotionBottle", transform);
            isModel = (visual != null);

            if (!isModel)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);
                visual.transform.localScale = Vector3.one * visualSize;
            }
            else
            {
                ModelLoader.StripColliders(visual);
                // KayKit-бутылочка ~0.4 ед. в высоту, увеличим чтобы было читаемо.
                visual.transform.localScale = Vector3.one * 1.5f;
            }

            visual.name = "CarryVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = new Vector3(0f, visualHeight, 0f);
            visual.SetActive(false);
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

            ModelLoader.Tint(visual, color, isModel ? 0.6f : 1f);
        }
    }
}