using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Rewards
{
    /// <summary>
    /// The rewards an enemy grants when killed. Authored in the enemy JSON under "Rewards".
    /// </summary>
    [Serializable]
    public class RewardTable
    {
        public int Exp;
        public int Gold;
        public List<DropEntry> Drops = new List<DropEntry>();
    }
}
