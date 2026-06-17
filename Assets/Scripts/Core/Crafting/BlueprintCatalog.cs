using System.Collections.Generic;
using DwarfsCrypt.Domain.Crafting;

namespace Core.Crafting
{
    /// <summary>
    /// In-memory list of every crafting blueprint, loaded once from
    /// StreamingAssets/Crafting/blueprints.json. Mirrors <c>ItemCatalog</c>.
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

            var collection = BlueprintConfigLoader.LoadFromStreamingAssets(BlueprintsPath);
            if (collection?.Blueprints == null) return;

            foreach (var bp in collection.Blueprints)
            {
                if (bp == null || string.IsNullOrEmpty(bp.Id)) continue;
                _all.Add(bp);
                _byId[bp.Id] = bp;
            }
        }

        public static bool TryGet(string id, out Blueprint blueprint)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out blueprint);
        }

        public static Blueprint Get(string id) => TryGet(id, out var bp) ? bp : null;
    }
}
