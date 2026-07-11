using System;
using UnityEngine;

namespace DwarfsCrypt.Domain.Characters
{
    public class Character : CharacterBase
    {
        public bool IsDead { get; private set; }

        public event Action<float, float> OnDamaged;       // currentHp, maxHp
        public event Action<float, bool> OnDamageTaken;     // damageAmount, isCrit
        public event Action<float, float> OnManaChanged;    // currentMana, maxMana
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

        /// <summary>Spends mana if there is enough; returns false (and spends nothing) otherwise.
        /// A zero/negative cost always succeeds so free skills need no special-casing.</summary>
        public bool TrySpendMana(float amount)
        {
            if (amount <= 0f) return true;
            if (IsDead || CurrentMana < amount) return false;

            CurrentMana -= amount;
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (IsDead || amount <= 0f || CurrentMana >= MaxMana) return;

            CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        /// <summary>Passive regeneration driven by the ManaRegen attribute (MEN-based).
        /// Call once per frame with the frame's delta time.</summary>
        public void RegenerateMana(float deltaTime)
        {
            RestoreMana(Attributes.GetFinal(AttributeType.ManaRegen) * deltaTime);
        }

        private void Die()
        {
            IsDead = true;
            OnDied?.Invoke();
        }
    }
}
