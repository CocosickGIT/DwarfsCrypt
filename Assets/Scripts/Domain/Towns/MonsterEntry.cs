using System;

namespace DwarfsCrypt.Domain.Towns
{
    /// <summary>
    /// One mob in a zone's roster, as authored in towns.json. Says *what* spawns; how many and where
    /// stays with the scene's camp zones, because a camp layout is level design while the roster is
    /// town flavour — that split is what lets one forest scene serve several towns.
    /// </summary>
    [Serializable]
    public class MonsterEntry
    {
        /// <summary>Stable id, resolved to a prefab by the zone scene's MonsterRegistry.</summary>
        public string MonsterId;

        /// <summary>
        /// Resources path (no extension) of this mob's character json, e.g. "Characters/ice_wraith".
        /// This is the mob's real identity — stats, level and drops. Empty falls back to whatever the
        /// registry entry declares, so a roster can reuse one prefab with several stat blocks.
        /// </summary>
        public string ConfigPath;

        /// <summary>Relative spawn weight. Higher = more common; player Luck compresses the spread.</summary>
        public float Weight = 1f;
    }
}
