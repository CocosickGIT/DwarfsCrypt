using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Presentation
{
    /// <summary>
    /// Cached <see cref="PlayerState"/> → string names. Enum <c>ToString()</c> is reflection-based
    /// and allocates a new string on every call; the animation code hits it every frame per unit
    /// (HasAnimation / SPUM key lookups), so caching the names removes that per-frame GC churn.
    /// </summary>
    public static class SpumStateNames
    {
        private static readonly Dictionary<PlayerState, string> _names = BuildNames();

        private static Dictionary<PlayerState, string> BuildNames()
        {
            var map = new Dictionary<PlayerState, string>();
            foreach (PlayerState state in Enum.GetValues(typeof(PlayerState)))
                map[state] = state.ToString();
            return map;
        }

        /// <summary>The cached string name for a state (equivalent to <c>state.ToString()</c>, no allocation).</summary>
        public static string Name(PlayerState state) =>
            _names.TryGetValue(state, out var name) ? name : state.ToString();
    }
}
