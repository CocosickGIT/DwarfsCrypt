using System;
using UnityEngine;

namespace DwarfsCrypt.Domain.Characters
{
    public class Character : CharacterBase
    {
        public bool IsDead { get; private set; }

        public event Action<float, float> OnDamaged;       // currentHp, maxHp
        public event Action<float, bool> OnDamageTaken;     // damageAmount, isCrit
        public event Action OnDied;

        public Character(CharacterConfig config) : base(config) { }

        public void TakeDamage(float damage, bool isCrit = false)
        {
            if (IsDead || damage <= 0f) return;

            // Round to a whole number so HP stays integer and the floating damage
            // number matches the HP actually lost (minimum 1 damage per hit).
            float applied = Math.Max(1f, (float)Math.Round(damage, MidpointRounding.AwayFromZero));

            CurrentHp = Math.Max(0f, CurrentHp - applied);
            OnDamageTaken?.Invoke(applied, isCrit);
            OnDamaged?.Invoke(CurrentHp, MaxHp);

            Debug.Log($"{this.Name} take {applied} damage  CurrentHp= {CurrentHp}");

            if (CurrentHp <= 0f)
            {
                Die();
                Debug.Log("IsDEAD= " + $"{IsDead}");
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            float applied = (float)Math.Round(amount, MidpointRounding.AwayFromZero);
            CurrentHp = Math.Min(MaxHp, CurrentHp + applied);
            OnDamaged?.Invoke(CurrentHp, MaxHp);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
