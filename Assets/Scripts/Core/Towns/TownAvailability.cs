using System.Collections.Generic;

namespace Core.Towns
{
    /// <summary>
    /// The one rule deciding whether town-scoped content — a forge recipe, a quest — is offered in a
    /// given town. Content carries the tag (a <c>Towns</c> list) rather than the town carrying a list
    /// of content ids: adding a blueprint or quest then touches one place and can never leave a
    /// dangling id behind in towns.json.
    ///
    /// <b>An empty or absent list means "offered everywhere."</b> That default is what keeps this
    /// cheap — shared baseline content (iron ingots, the opening quests) stays untagged and shows up
    /// at every station and board, and only genuine local flavour names its towns.
    /// </summary>
    public static class TownAvailability
    {
        public static bool IsOfferedIn(List<string> towns, string townId)
        {
            if (towns == null || towns.Count == 0) return true;
            if (string.IsNullOrEmpty(townId)) return false;

            for (int i = 0; i < towns.Count; i++)
                if (towns[i] == townId)
                    return true;

            return false;
        }

        /// <summary>Availability in the town the hub is currently showing.</summary>
        public static bool IsOfferedHere(List<string> towns) =>
            IsOfferedIn(towns, TownContext.ActiveTownId);
    }
}
