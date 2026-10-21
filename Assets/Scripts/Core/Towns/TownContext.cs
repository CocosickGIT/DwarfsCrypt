using Core.Player;
using DwarfsCrypt.Domain.Towns;

namespace Core.Towns
{
    /// <summary>
    /// The town the player is currently standing in (the hub). Set when a town scene is entered and
    /// read by town-scene systems — the environment builder, the Gate, the quest board. Static so it
    /// survives the scene load the same way <see cref="RunContext"/> does, and mirrored onto the
    /// player's profile (<see cref="DwarfsCrypt.Domain.Player.PlayerProfile.CurrentTownId"/>) so it
    /// also survives quitting the game.
    ///
    /// Distinction: <see cref="RunContext"/> is the town a *run* launched from (carried into a game
    /// scene to apply modifiers); <see cref="TownContext"/> is the town the *hub* is showing right now.
    /// One shared hub scene serves every town by reading this on load.
    /// </summary>
    public static class TownContext
    {
        /// <summary>Id of the town currently being shown (null before any town is entered).</summary>
        public static string CurrentTownId { get; private set; }

        /// <summary>The current town's config, or null when unset/unknown.</summary>
        public static TownConfig Current =>
            TownCatalog.TryGet(CurrentTownId, out var town) ? town : null;

        /// <summary>
        /// The town to act for right now: the entered town, falling back to the default (starting)
        /// town when nothing has been entered. Use this — not <see cref="CurrentTownId"/> — when
        /// resolving town-scoped content, so a window opened in a bare-scene playtest still resolves
        /// to a real town instead of filtering everything town-tagged away.
        /// </summary>
        public static string ActiveTownId =>
            !string.IsNullOrEmpty(CurrentTownId) ? CurrentTownId : TownCatalog.DefaultTownId;

        /// <summary>
        /// Travel to a town: makes it current and records it on the active profile so the player
        /// resumes here next session. Unknown/empty ids fall back to the default (starting) town so
        /// the hub always has a valid town to build.
        /// </summary>
        public static void Enter(string townId)
        {
            Set(townId);

            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;
            if (profile == null || profile.CurrentTownId == CurrentTownId) return;

            profile.CurrentTownId = CurrentTownId;
            PlayerProfileService.Save();
        }

        /// <summary>
        /// Make a town current *without* writing it back to the profile. For adopting a town that is
        /// already the profile's own (see <see cref="RestoreFromProfile"/>) and for the editor's
        /// bare-scene fallback, which must not overwrite a real save's town.
        /// </summary>
        public static void Set(string townId)
        {
            CurrentTownId = TownCatalog.TryGet(townId, out _) ? townId : TownCatalog.DefaultTownId;
        }

        /// <summary>
        /// Resume in the town the active profile was last saved in. A new game — or a save whose town
        /// no longer exists in towns.json — starts in the default town.
        /// </summary>
        public static void RestoreFromProfile()
        {
            PlayerProfileService.EnsureLoaded();
            Set(PlayerProfileService.Current?.CurrentTownId);
        }
    }
}
