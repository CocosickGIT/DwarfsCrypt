using System;
using System.Collections.Generic;
using DwarfsCrypt.Domain.Rewards;

namespace DwarfsCrypt.Domain.Characters
{
    public abstract class CharacterBase
    {
        public string Name { get; protected set; }
        public Race Race { get; protected set; }
        public int Level { get; protected set; }
        public CharacterStats Stats { get; protected set; }
        public CharacterAttributes Attributes { get; protected set; }

        public int MaxHp      => (int)Attributes.GetFinal(AttributeType.MaxHp);
        public int MaxStamina => (int)Attributes.GetFinal(AttributeType.MaxStamina);
        public int MaxMana    => (int)Attributes.GetFinal(AttributeType.MaxMana);

        public float CurrentHp      { get; protected set; }
        public float CurrentStamina { get; protected set; }
        public float CurrentMana    { get; protected set; }

        public IReadOnlyList<string> Equipment { get; protected set; }

        public RewardTable Rewards { get; protected set; }

        public IReadOnlyList<BossAttackPattern> BossAttacks { get; protected set; }

        protected CharacterBase(CharacterConfig config)
        {
            ApplyConfig(config);
        }

        protected void ApplyConfig(CharacterConfig config)
        {
            Name  = config.Name;
            Race  = (Race)Enum.Parse(typeof(Race), config.Race, ignoreCase: true);
            Level = config.Level;
            Stats = config.Stats;

            Attributes = new CharacterAttributes(config.Stats, AttributeFormulas.Default(config.MaxHp, config.MaxStamina, config.MaxMana));

            CurrentHp      = MaxHp;
            CurrentStamina = MaxStamina;
            CurrentMana    = MaxMana;

            Equipment   = config.Equipment ?? new List<string>();
            Rewards     = config.Rewards ?? new RewardTable();
            BossAttacks = config.BossAttacks ?? new List<BossAttackPattern>();
        }
    }
}
