using System;

namespace DwarfsCrypt.Domain.Rewards
{
    /// <summary>
    /// One possible item drop in an enemy's reward table. Authored in the enemy JSON.
    /// </summary>
    [Serializable]
    public class DropEntry
    {
        public string ItemId;

        [UnityEngine.Tooltip("Probability this item drops, 0..1.")]
        public float Chance = 1f;

        public int MinQuantity = 1;
        public int MaxQuantity = 1;
    }
}
