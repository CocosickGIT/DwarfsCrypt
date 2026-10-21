using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// A crafting recipe: consume the listed <see cref="Ingredients"/> to produce
    /// <see cref="ResultQuantity"/> of <see cref="ResultItemId"/> at a given <see cref="Station"/>.
    /// </summary>
    [Serializable]
    public class Blueprint
    {
        public string Id;
        public string Name;
        public string ResultItemId;
        public int ResultQuantity = 1;
        public List<CraftIngredient> Ingredients = new List<CraftIngredient>();

        // String rather than the enum for the same JsonUtility reason as QuestDefinition.Category:
        // "Forge" / "Workshop" reads far better in the authored json than a bare 0/1.
        public string Station = "Forge";

        [UnityEngine.Tooltip("True when the recipe has to be learned from a blueprint earned in a " +
                             "quest or mission. Forge recipes leave this false — they are common " +
                             "smithing knowledge and available from the start.")]
        public bool RequiresLearning;

        [UnityEngine.Tooltip("Crafting mastery level the player needs before this recipe can be worked.")]
        public int RequiredMastery = 1;

        [UnityEngine.Tooltip("Crafting mastery experience granted per successful craft.")]
        public int MasteryExp = 10;

        [UnityEngine.Tooltip("Whether the result's quality is rolled against crafting mastery. " +
                             "Only ever applies to equippable results — materials always come out plain.")]
        public bool AllowQuality = true;

        [UnityEngine.Tooltip("Ids of the towns whose station offers this recipe. Empty = every town " +
                             "(shared smithing knowledge). List towns to make a recipe local flavour, " +
                             "e.g. only Frostmere's smith works frost steel.")]
        public List<string> Towns;

        public CraftStation StationValue =>
            Enum.TryParse<CraftStation>(Station, ignoreCase: true, out var s) ? s : CraftStation.Forge;
    }
}
