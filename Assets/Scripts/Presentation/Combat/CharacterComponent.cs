using System;
using UnityEngine;
using Core.Characters;
using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Presentation.Combat
{
    // Single source of truth for a character's data on a GameObject.
    // _configPath is used when the component self-initializes (Inspector workflow).
    // CharacterFactory calls Initialize() before activation for spawner-driven units.
    public class CharacterComponent : MonoBehaviour, IDamageable
    {
        [Tooltip("Path relative to Resources, no extension. E.g. Characters/dwarf_warrior")]
        [SerializeField] private string _configPath;

        public Character Character { get; private set; }

        public CharacterAttributes Attributes => Character.Attributes;
        public float CurrentHp => Character.CurrentHp;
        public int MaxHp => Character.MaxHp;
        public bool IsDead => Character.IsDead;

        public event Action<float, float> OnDamaged;       // currentHp, maxHp
        public event Action<float, bool> OnDamageTaken;     // damageAmount, isCrit
        public event Action<float, float> OnManaChanged;    // currentMana, maxMana
        public event Action OnDied;

        public float CurrentMana => Character.CurrentMana;
        public int MaxMana => Character.MaxMana;

        private void Awake()
        {
            if (Character != null) return; // already initialized by CharacterFactory
            if (!string.IsNullOrEmpty(_configPath))
                Initialize(_configPath);
        }

        public void Initialize(string configPath) =>
            Initialize(CharacterConfigLoader.LoadFromResources(configPath));

        public void Initialize(CharacterConfig config)
        {
            if (Character != null)
            {
                Character.OnDamaged -= PropagateOnDamaged;
                Character.OnDamageTaken -= PropagateOnDamageTaken;
                Character.OnManaChanged -= PropagateOnManaChanged;
                Character.OnDied -= PropagateOnDied;
            }
            Character = new Character(config);
            Character.OnDamaged += PropagateOnDamaged;
            Character.OnDamageTaken += PropagateOnDamageTaken;
            Character.OnManaChanged += PropagateOnManaChanged;
            Character.OnDied += PropagateOnDied;
        }

        private void PropagateOnDamaged(float hp, float max) => OnDamaged?.Invoke(hp, max);
        private void PropagateOnDamageTaken(float amount, bool isCrit) => OnDamageTaken?.Invoke(amount, isCrit);
        private void PropagateOnManaChanged(float mana, float max) => OnManaChanged?.Invoke(mana, max);
        private void PropagateOnDied() => OnDied?.Invoke();

        public void TakeDamage(float damage, bool isCrit = false) => Character.TakeDamage(damage, isCrit);
        public void Heal(float amount) => Character.Heal(amount);
    }
}
