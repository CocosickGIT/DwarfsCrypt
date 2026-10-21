namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// Where a blueprint can be worked. The two stations play different roles:
    ///
    /// <list type="bullet">
    /// <item><b>Forge</b> — the town's standing smithy. Recipes are known from the start and turn raw
    /// drops into the basic materials everything else is built from (ore to ingots, hides to leather).
    /// Output is deterministic: an ingot is an ingot.</item>
    /// <item><b>Workshop</b> — the player's own bench. Recipes must be learned from blueprints earned
    /// through quests and missions, and the result's quality is rolled against crafting mastery, so the
    /// same blueprint can yield anything from a plain to a legendary version of the item.</item>
    /// </list>
    /// </summary>
    public enum CraftStation
    {
        Forge = 0,
        Workshop = 1
    }
}
