using System;
using UnityEngine;
using Core.Characters;
using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Presentation.Combat
{
    // Single source of truth for a character's data on a GameObject.
    // Add to player and enemy prefabs; set Config Path in the Inspector.
    public class CharacterComponent : MonoBehaviour, IDamageable
    {
        [Tooltip("Path relative to StreamingAssets, no extension. E.g. Characters/dwarf_warrior")]
        [SerializeField] private string _configPath;

        public Character Character { get; private set; }

        public CharacterAttributes Attributes => Character.Attributes;
        public float CurrentHp => Character.CurrentHp;
        public int MaxHp => Character.MaxHp;
        public bool IsDead => Character.IsDead;

        public event Action<float, float> OnDamaged; // currentHp, maxHp
        public event Action OnDied;

        private void Awake()
        {
            CharacterConfig config = CharacterConfigLoader.LoadFromStreamingAssets(_configPath);
            Character = new Character(config);
            Character.OnDamaged += (current, max) => OnDamaged?.Invoke(current, max);
            Character.OnDied    += ()           => OnDied?.Invoke();
        }

        public void TakeDamage(float damage) => Character.TakeDamage(damage);
        public void Heal(float amount)       => Character.Heal(amount);
    }
}
