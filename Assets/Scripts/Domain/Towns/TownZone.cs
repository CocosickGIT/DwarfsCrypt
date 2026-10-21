using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Towns
{
    /// <summary>
    /// One destination the town's Gate offers: a game scene the player can enter, plus the
    /// metadata the Gate UI needs. The town's <see cref="TownConfig.Modifiers"/> apply to every
    /// zone it leads to.
    /// </summary>
    [Serializable]
    public class TownZone
    {
        /// <summary>Stable id, unique within its town (e.g. "forest_edge"). This — not the scene name —
        /// is what a cleared zone is recorded under, so two zones may share a scene and renaming a
        /// scene never loses progress. Required for clear-tracking; zones without one can be entered
        /// but never count toward unlocking the next town.</summary>
        public string Id;

        /// <summary>Scene to load (must be in Build Settings). Mirrors the old LevelIcon.sceneName.</summary>
        public string Scene;

        /// <summary>Display name shown on the Gate icon/button.</summary>
        public string Name;

        /// <summary>Minimum player level suggested for this zone (for display/gating).</summary>
        public int MinLevel;

        /// <summary>Resources path (no extension) of the mob this zone farms in an AFK run.
        /// Empty = AFK disabled for this zone. Mirrors the old LevelIcon.afkEnemyConfigPath.</summary>
        public string AfkEnemyConfigPath;

        /// <summary>
        /// The mobs that spawn in this zone. Empty/omitted leaves the scene's camp zones spawning
        /// whatever they were authored with — so existing scenes keep working untouched.
        ///
        /// This is what makes one scene serve several towns: Ironhold's "forest_edge" and Frostmere's
        /// "frozen_pass" can both load 1_1_Forest and differ entirely in what lives there.
        /// </summary>
        public List<MonsterEntry> MonsterTable;
    }
}
