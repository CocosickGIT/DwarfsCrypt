using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Characters
{
    public class CharacterAttributes
    {
        private static readonly int Count = Enum.GetValues(typeof(AttributeType)).Length;

        private readonly float[]               _base      = new float[Count];
        private readonly float?[]              _cache     = new float?[Count];
        private readonly List<StatModifier>[]  _modifiers;

        public event Action<AttributeType> OnChanged;

        public CharacterAttributes(CharacterStats stats, AttributeFormulas formulas)
        {
            _modifiers = new List<StatModifier>[Count];
            for (int i = 0; i < Count; i++)
                _modifiers[i] = new List<StatModifier>();

            foreach (var (attr, formula) in formulas.All)
                Set(attr, formula(stats));
        }

        private void Set(AttributeType attr, float value)
        {
            _base[(int)attr] = value;
            Invalidate(attr);
        }

        private void Invalidate(AttributeType attr) => _cache[(int)attr] = null;

        public float GetBase(AttributeType attr) => _base[(int)attr];

        public float GetFinal(AttributeType attr)
        {
            int i = (int)attr;
            if (_cache[i].HasValue) return _cache[i].Value;

            float flat         = _base[i];
            float percentBase  = 0f;
            float percentFinal = 0f;

            foreach (var mod in _modifiers[i])
            {
                switch (mod.Type)
                {
                    case ModifierType.Flat:         flat         += mod.Value; break;
                    case ModifierType.PercentBase:  percentBase  += mod.Value; break;
                    case ModifierType.PercentFinal: percentFinal += mod.Value; break;
                }
            }

            float result = flat * (1f + percentBase) * (1f + percentFinal);
            _cache[i] = result;
            return result;
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers[(int)modifier.Attribute].Add(modifier);
            Invalidate(modifier.Attribute);
            OnChanged?.Invoke(modifier.Attribute);
        }

        public void RemoveModifiersFromSource(string source)
        {
            var affected = new HashSet<AttributeType>();

            for (int i = 0; i < Count; i++)
            {
                var list = _modifiers[i];
                for (int j = list.Count - 1; j >= 0; j--)
                {
                    if (list[j].Source != source) continue;
                    list.RemoveAt(j);
                    affected.Add((AttributeType)i);
                }
            }

            foreach (var attr in affected)
            {
                Invalidate(attr);
                OnChanged?.Invoke(attr);
            }
        }
    }
}
