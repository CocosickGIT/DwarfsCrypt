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

        [UnityEngine.Tooltip("Ids of workshop blueprints taught by this reward. Unlike drops these " +
                             "are always granted — a blueprint is the point of the quest, not a " +
                             "lottery — and re-earning one the player already knows does nothing.")]
        public List<string> Blueprints = new List<string>();
    }
}
