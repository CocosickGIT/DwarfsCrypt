using System;
using UnityEngine;

namespace DwarfsCrypt.Domain.Characters
{
    public class Character : CharacterBase
    {
        public bool IsDead { get; private set; }

        public event Action<float, float> OnDamaged; // currentHp, maxHp
        public event Action OnDied;

        public Character(CharacterConfig config) : base(config) { }

        public void TakeDamage(float damage)
        {
            if (IsDead || damage <= 0f) return;

            CurrentHp = Math.Max(0f, CurrentHp - damage);
            OnDamaged?.Invoke(CurrentHp, MaxHp);

            Debug.Log($"{this.Name} " + " CurrentHp= " + $"{CurrentHp}");

            if (CurrentHp <= 0f)
            {
                Die();
                Debug.Log( "IsDEAD= "+$"{IsDead}");
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            OnDamaged?.Invoke(CurrentHp, MaxHp);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
