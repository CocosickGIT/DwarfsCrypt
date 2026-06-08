using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


namespace DwarfsCrypt.Domain.Characters
{
    public class AttributeFormulas
    {
        private readonly Dictionary<AttributeType, Func<CharacterStats, float>> _formulas = new();

        public IReadOnlyDictionary<AttributeType, Func<CharacterStats, float>> All => _formulas;

        public AttributeFormulas Register(AttributeType attr, Func<CharacterStats, float> formula)
        {
            _formulas[attr] = formula;
            return this;
        }

        public static AttributeFormulas Default(int baseMaxHp, int baseMaxStamina, int baseMaxMana) =>
            new AttributeFormulas()
                // Primary
                .Register(AttributeType.Strength,        s => s.Str)
                .Register(AttributeType.Dexterity,       s => s.Dex)
                .Register(AttributeType.Constitution,    s => s.Con)
                .Register(AttributeType.Wit,             s => s.Wit)
                .Register(AttributeType.Mentality,       s => s.Men)
                .Register(AttributeType.Luck,            s => s.Luc)
                // Resources
                .Register(AttributeType.MaxHp,           s => baseMaxHp      + s.Con * 10f)
                .Register(AttributeType.MaxStamina,      s => baseMaxStamina + s.Con * 5f)
                .Register(AttributeType.MaxMana,         s => baseMaxMana    + s.Men * 5f)
                // Offense
                .Register(AttributeType.PhysicalAttack,  CalcPhysicalAttack)
                .Register(AttributeType.MagicAttack,     s => s.Wit * 2f)
                .Register(AttributeType.AttackSpeed,     s => 1f   + s.Dex * 0.01f)
                .Register(AttributeType.CritChance,      CalcCritChance)
                .Register(AttributeType.CritDamage,      CalcCritDamage)
                // Defense
                .Register(AttributeType.PhysicalDefense, s => s.Con * 1f)
                .Register(AttributeType.MagicDefense,    s => s.Men * 1f)
                // Movement
                .Register(AttributeType.MoveSpeed,       s => 5f   + s.Dex * 0.05f);

        private static float CalcCritDamage(CharacterStats s) => Mathf.Max(1,s.Luc * 0.1f);
        private static float CalcCritChance(CharacterStats s) => s.Luc;
        private static float CalcPhysicalAttack(CharacterStats s) => s.Str * 2f;

        public static float RollPhysicalDamage(CharacterAttributes attrs)
        {
            float str    = attrs.GetFinal(AttributeType.Strength);
            float luc    = attrs.GetFinal(AttributeType.Luck);
            float dmg    = attrs.GetFinal(AttributeType.CritDamage);
            
            float physAtk   = str;
            float critChance = luc;
            float critDmg    = dmg;

            return Random.value * 100f < critChance
                ? physAtk * critDmg
                : physAtk;
        }


    }
}