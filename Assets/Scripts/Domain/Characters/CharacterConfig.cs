using System;
using System.Collections.Generic;
using DwarfsCrypt.Domain.Rewards;

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

        // Rewards granted when this character is killed. Authored for enemies; null/ignored for the player.
        public RewardTable Rewards;
    }
}
