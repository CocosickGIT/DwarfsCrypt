using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Characters
{
    public abstract class CharacterBase
    {
        public string Name { get; protected set; }
        public Race Race { get; protected set; }
        public int Level { get; protected set; }
        public CharacterStats Stats { get; protected set; }

        public int MaxHp { get; protected set; }
        public int MaxStamina { get; protected set; }
        public int MaxMana { get; protected set; }

        public int CurrentHp { get; protected set; }
        public int CurrentStamina { get; protected set; }
        public int CurrentMana { get; protected set; }

        public IReadOnlyList<string> Equipment { get; protected set; }

        protected CharacterBase(CharacterConfig config)
        {
            ApplyConfig(config);
        }

        protected void ApplyConfig(CharacterConfig config)
        {
            Name = config.Name;
            Race = (Race)Enum.Parse(typeof(Race), config.Race, ignoreCase: true);
            Level = config.Level;
            Stats = config.Stats;
            MaxHp = config.MaxHp;
            MaxStamina = config.MaxStamina;
            MaxMana = config.MaxMana;
            CurrentHp = MaxHp;
            CurrentStamina = MaxStamina;
            CurrentMana = MaxMana;
            Equipment = config.Equipment ?? new List<string>();
        }
    }
}
