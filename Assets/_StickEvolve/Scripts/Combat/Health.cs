using System;
using UnityEngine;

namespace StickEvolve.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitWorldPos);
        bool IsAlive { get; }
    }

    /// <summary>
    /// Универсальное здоровье. Сидит на Hero и Enemy.
    /// OnDeath срабатывает один раз за «жизнь». После ревайва через Configure
    /// (CurrentHp снова > 0) OnDeath сможет сработать заново на следующей смерти.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 10f;

        public float MaxHp => maxHp;
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f;

        public event Action<float, Vector3> OnDamaged; // damage, hit world position
        public event Action OnDeath;
        public event Action<float, float> OnHpChanged; // current, max

        private bool _deathFired;

        public void Configure(float newMax, bool fullHeal = true)
        {
            maxHp = Mathf.Max(1f, newMax);
            if (fullHeal) CurrentHp = maxHp;
            else CurrentHp = Mathf.Min(CurrentHp, maxHp);
            if (CurrentHp > 0f) _deathFired = false;
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        private void Awake()
        {
            if (CurrentHp <= 0f) CurrentHp = maxHp;
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }

        public void TakeDamage(float amount, Vector3 hitWorldPos)
        {
            if (!IsAlive || amount <= 0f) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            OnDamaged?.Invoke(amount, hitWorldPos);
            OnHpChanged?.Invoke(CurrentHp, maxHp);
            if (CurrentHp <= 0f && !_deathFired)
            {
                _deathFired = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, maxHp);
        }
    }
}
