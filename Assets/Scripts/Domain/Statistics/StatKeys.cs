namespace DwarfsCrypt.Domain.Statistics
{
    /// <summary>
    /// Canonical statistic-key strings shared by the statistics tracker and the quest system.
    /// A key is either a bare category total (e.g. "kill") or a category scoped to a target
    /// (e.g. "kill:Skeleton", "craft:bp_iron_sword"). Keeping the format in one place means the
    /// recorder (StatisticsService) and the reader (quest objectives) can never drift apart.
    /// </summary>
    public static class StatKeys
    {
        public const string KillPrefix = "kill";
        public const string CraftPrefix = "craft";

        public static string Kill(string targetId) => Scoped(KillPrefix, targetId);
        public static string Craft(string targetId) => Scoped(CraftPrefix, targetId);

        private static string Scoped(string prefix, string targetId) =>
            string.IsNullOrEmpty(targetId) ? prefix : prefix + ":" + targetId;
    }
}
