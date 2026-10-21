using System.Collections.Generic;
using Core.Towns;
using DwarfsCrypt.Domain.Crafting;

namespace Core.Crafting
{
    /// <summary>
    /// In-memory list of every crafting blueprint, loaded once from
    /// Resources/Crafting/blueprints.json. Mirrors <c>ItemCatalog</c>.
    /// </summary>
    public static class BlueprintCatalog
    {
        private const string BlueprintsPath = "Crafting/blueprints";

        private static List<Blueprint> _all;
        private static Dictionary<string, Blueprint> _byId;

        public static IReadOnlyList<Blueprint> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static void EnsureLoaded()
        {
            if (_all != null) return;

            _all = new List<Blueprint>();
            _byId = new Dictionary<string, Blueprint>();

            var collection = BlueprintConfigLoader.LoadFromResources(BlueprintsPath);
            if (collection?.Blueprints == null) return;

            foreach (var bp in collection.Blueprints)
            {
                if (bp == null || string.IsNullOrEmpty(bp.Id)) continue;
                _all.Add(bp);
                _byId[bp.Id] = bp;
            }
        }

        /// <summary>
        /// Every blueprint one town's station works, in authored order. Untagged recipes are shared
        /// smithing knowledge and appear at every town — see <see cref="TownAvailability"/>.
        ///
        /// This filters what a station *shows*; it is deliberately not enforced in
        /// <see cref="CraftingService"/>, so anything resolving a blueprint by id (quest rewards, AFK
        /// runs, the reward screen) keeps working from any town.
        /// </summary>
        public static List<Blueprint> ForStation(CraftStation station, string townId)
        {
            EnsureLoaded();

            var matching = new List<Blueprint>();
            foreach (var bp in _all)
                if (bp.StationValue == station && TownAvailability.IsOfferedIn(bp.Towns, townId))
                    matching.Add(bp);

            return matching;
        }

        public static bool TryGet(string id, out Blueprint blueprint)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out blueprint);
        }

        public static Blueprint Get(string id) => TryGet(id, out var bp) ? bp : null;
    }
}
