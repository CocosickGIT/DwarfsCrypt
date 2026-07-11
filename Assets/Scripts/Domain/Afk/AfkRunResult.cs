namespace DwarfsCrypt.Domain.Afk
{
    /// <summary>
    /// Outcome of a single AFK Farming run: a fight simulation of the player against a fixed
    /// number of mobs. The run is bounded by a constant duration and ends early when either the
    /// mob queue is exhausted (<see cref="ClearedAll"/>) or the player's health reaches zero.
    /// </summary>
    public class AfkRunResult
    {
        /// <summary>Number of mobs queued for this run (the target).</summary>
        public int MobCount;

        /// <summary>How many mobs the player actually killed before the run ended.</summary>
        public int Kills;

        /// <summary>False when the player's health hit zero during the simulation.</summary>
        public bool PlayerSurvived;

        /// <summary>Simulated seconds elapsed when the run ended.</summary>
        public float DurationUsed;

        /// <summary>True when every queued mob was killed.</summary>
        public bool ClearedAll => Kills >= MobCount;
    }
}
