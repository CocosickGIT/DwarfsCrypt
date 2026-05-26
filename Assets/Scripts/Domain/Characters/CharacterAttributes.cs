using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Characters
{
    public class CharacterAttributes
    {
        private static readonly int Count = Enum.GetValues(typeof(AttributeType)).Length;

        private readonly float[] _base = new float[Count];
        private readonly List<StatModifier> _modifiers = new();

        public event Action<AttributeType> OnChanged;

        public CharacterAttributes(CharacterStats stats, int baseMaxHp, int baseMaxStamina, int baseMaxMana)
        {
            BuildBase(stats, baseMaxHp, baseMaxStamina, baseMaxMana);
        }

        private void BuildBase(CharacterStats s, int hp, int stamina, int mana)//additional class with formulas(I, II)
        {
            Set(AttributeType.Strength,       s.Str);
            Set(AttributeType.Dexterity,      s.Dex);
            Set(AttributeType.Constitution,   s.Con);
            Set(AttributeType.Wit,            s.Wit);
            Set(AttributeType.Mentality,      s.Men);
            Set(AttributeType.Luck,           s.Luc);

            Set(AttributeType.MaxHp,          hp      + s.Con * 10f);
            Set(AttributeType.MaxStamina,     stamina + s.Con * 5f);
            Set(AttributeType.MaxMana,        mana    + s.Men * 5f);

            Set(AttributeType.PhysicalAttack, s.Str * 2f);
            Set(AttributeType.MagicAttack,    s.Wit * 2f);
            Set(AttributeType.PhysicalDefense,s.Con * 1f);
            Set(AttributeType.MagicDefense,   s.Men * 1f);

            Set(AttributeType.AttackSpeed,    1f  + s.Dex * 0.01f);
            Set(AttributeType.MoveSpeed,      5f  + s.Dex * 0.05f);
            Set(AttributeType.CritChance,     s.Luc * 0.5f);
            Set(AttributeType.CritDamage,     150f + s.Luc * 1f);
        }

        private void Set(AttributeType attr, float value) => _base[(int)attr] = value;

        public float GetBase(AttributeType attr) => _base[(int)attr];

        public float GetFinal(AttributeType attr)
        {
            float flat        = _base[(int)attr];
            float percentBase = 0f;
            float percentFinal = 0f;

            foreach (var mod in _modifiers)
            {
                if (mod.Attribute != attr) continue;
                switch (mod.Type)
                {
                    case ModifierType.Flat:         flat         += mod.Value; break;
                    case ModifierType.PercentBase:  percentBase  += mod.Value; break;
                    case ModifierType.PercentFinal: percentFinal += mod.Value; break;
                }
            }

            return flat * (1f + percentBase) * (1f + percentFinal);
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            OnChanged?.Invoke(modifier.Attribute);
        }

        public void RemoveModifiersFromSource(string source)
        {
            var affected = new HashSet<AttributeType>();
            _modifiers.RemoveAll(m =>
            {
                if (m.Source != source) return false;
                affected.Add(m.Attribute);
                return true;
            });

            foreach (var attr in affected)
                OnChanged?.Invoke(attr);
        }
    }
}
