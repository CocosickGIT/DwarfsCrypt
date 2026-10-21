using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Towns
{
    [Serializable]
    public class TownConfig
    {
        public string Id;
        public string Name;

        /// <summary>Scene name of this town's hub (must be in Build Settings). Loaded when the player
        /// returns to town from one of its zones. With a single shared hub scene this is the same
        /// value for every town; it stays per-town so a town can opt into its own scene. e.g. "1_Town".</summary>
        public string Scene;

        /// <summary>Resources path (no extension) of this town's Environment prefab — the swappable
        /// visual layer (background art, colliders, NPC placements) the shared hub scene instantiates
        /// for this town. Empty = bare scene. e.g. "Towns/Environments/Ironhold".</summary>
        public string EnvironmentPrefab;

        public LevelBand LevelBand;

        /// <summary>Where this town's node sits on the world map, in normalized coordinates
        /// (see <see cref="MapPoint"/>). Null/omitted = the map window lays the node out on a
        /// fallback row so a town is never invisible just because its position wasn't authored.</summary>
        public MapPoint MapPosition;

        /// <summary>
        /// Id of the town that gates this one: the player must clear every zone of that town before
        /// this one can be travelled to. Empty/omitted = open from the start (the starting town).
        /// The map window draws a connecting line along this relation.
        /// </summary>
        public string UnlockedBy;

        /// <summary>Run modifiers this town applies to every game scene its Gate leads to.</summary>
        public List<TownModifier> Modifiers;

        /// <summary>Scenes the town's Gate offers as entry points.</summary>
        public List<TownZone> Zones;
    }
}
