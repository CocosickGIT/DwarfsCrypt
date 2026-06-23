using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Quests
{
    public enum QuestState
    {
        Active,        // accepted, objectives not yet all met
        ReadyToClaim,  // all objectives met, waiting for the player to collect the reward
        Completed      // reward claimed (kept so daily/weekly cooldowns can be measured)
    }

    /// <summary>
    /// The player's snapshot of one objective at accept-time. Progress is always measured against the
    /// statistic value captured when the quest was taken (<see cref="Baseline"/>), so a brand-new
    /// "kill 100" quest starts at 0 even if the lifetime kill count is already in the hundreds.
    /// </summary>
    [Serializable]
    public class QuestObjectiveProgress
    {
        public string StatKey;
        public int Baseline;
        public int Required;
    }

    /// <summary>
    /// Persisted per-quest state on the player profile. The quest log holds at most one entry per
    /// quest id; re-accepting a daily/weekly quest overwrites its previous (completed) entry.
    /// </summary>
    [Serializable]
    public class QuestProgress
    {
        public string QuestId;
        public QuestState State = QuestState.Active;
        public List<QuestObjectiveProgress> Objectives = new List<QuestObjectiveProgress>();

        // DateTime.UtcNow.Ticks at accept / completion. Used for daily & weekly availability.
        public long AcceptedAtTicks;
        public long CompletedAtTicks;
    }
}
