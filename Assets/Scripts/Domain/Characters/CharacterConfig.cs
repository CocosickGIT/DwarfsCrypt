using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Characters
{
    [Serializable]
    public class CharacterConfig
    {
        public string Name;
        public string Race;
        public int Level;
        public CharacterStats Stats;
        public int MaxHp;
        public int MaxStamina;
        public int MaxMana;
        public List<string> Equipment;
    }
}
