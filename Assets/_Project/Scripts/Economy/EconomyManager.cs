using System;
using Alchemy.Data;
using Alchemy.Utils;
using UnityEngine;

namespace Alchemy.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        [Header("Стартовые значения")]
        [SerializeField] private long startingGold = 0;
        [SerializeField] private int  startingGems = 0;

        [Header("Множители")]
        [SerializeField, Min(0.01f)] private float potionPriceMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float idleIncomeMultiplier  = 1f;

        public long Gold { get; private set; }
        public int  Gems { get; private set; }
        public float PotionPriceMultiplier => potionPriceMultiplier;
        public float IdleIncomeMultiplier  => idleIncomeMultiplier;

        public event Action<long>  OnGoldChanged;
        public event Action<int>   OnGemsChanged;
        public event Action<float> OnPotionPriceMultiplierChanged;
        public event Action<float> OnIdleIncomeMultiplierChanged;

                public void Init(SaveData save)
        {
            if (save != null && SaveSystem.Exists())
            {
                Gold = save.gold;
                Gems = save.gems;
                Debug.Log($"[EconomyManager] Загружен сейв: gold={Gold}, gems={Gems}");
            }
            else
            {
                Gold = startingGold;
                Gems = startingGems;
            }

            OnGoldChanged?.Invoke(Gold);
            OnGemsChanged?.Invoke(Gems);
            OnPotionPriceMultiplierChanged?.Invoke(potionPriceMultiplier);
            OnIdleIncomeMultiplierChanged?.Invoke(idleIncomeMultiplier);
        }

        public SaveData BuildSaveData()
        {
            return new SaveData
            {
                gold = Gold,
                gems = Gems
            };
        }

        public void AddGold(long amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpendGold(long amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void AddGems(int amount)
        {
            if (amount <= 0) return;
            Gems += amount;
            OnGemsChanged?.Invoke(Gems);
        }

        public bool TrySpendGems(int amount)
        {
            if (amount <= 0) return true;
            if (Gems < amount) return false;
            Gems -= amount;
            OnGemsChanged?.Invoke(Gems);
            return true;
        }

        public void SetPotionPriceMultiplier(float value)
        {
            potionPriceMultiplier = Mathf.Max(0.01f, value);
            OnPotionPriceMultiplierChanged?.Invoke(potionPriceMultiplier);
        }

        public void SetIdleIncomeMultiplier(float value)
        {
            idleIncomeMultiplier = Mathf.Max(0.01f, value);
            OnIdleIncomeMultiplierChanged?.Invoke(idleIncomeMultiplier);
        }

        public long GetSellPrice(long basePrice)
            => (long)Mathf.Max(1f, basePrice * potionPriceMultiplier);
    }
}