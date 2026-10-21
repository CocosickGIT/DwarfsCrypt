using DwarfsCrypt.Domain.Items;

namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// What a single successful craft actually produced. Returned by <c>CraftingService.TryCraft</c>
    /// so the UI can report the rolled quality and any mastery level-up instead of just "it worked".
    /// </summary>
    public class CraftOutcome
    {
        /// <summary>The id that landed in the inventory — a quality variant id for rolled results.</summary>
        public string ItemId;

        public int Quantity;

        /// <summary>Quality rolled for this craft. Always Common for deterministic forge recipes.</summary>
        public ItemRarity Quality;

        public int MasteryExpGained;

        /// <summary>Crafting mastery levels gained from this craft (usually 0).</summary>
        public int MasteryLevelsGained;
    }
}
