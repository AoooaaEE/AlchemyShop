using System;

namespace StickEvolve.Core
{
    /// <summary>
    /// Золото прототипа. Просто счётчик с событием OnGoldChanged.
    /// </summary>
    public class StickEconomy
    {
        public long Gold { get; private set; }
        public event Action<long> OnGoldChanged;

        public void AddGold(long amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            OnGoldChanged?.Invoke(Gold);
        }

        public bool TrySpend(long amount)
        {
            if (amount <= 0) return true;
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }

        public void Reset(long startingGold = 0)
        {
            Gold = startingGold;
            OnGoldChanged?.Invoke(Gold);
        }
    }
}
