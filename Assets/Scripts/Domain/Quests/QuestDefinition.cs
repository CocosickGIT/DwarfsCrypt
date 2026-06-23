using System;
using System.Collections.Generic;
using DwarfsCrypt.Domain.Rewards;

namespace DwarfsCrypt.Domain.Quests
{
    /// <summary>
    /// How often a quest can be taken again. Normal quests are one-shot; Daily/Weekly become
    /// available again from the quest-giver once their period rolls over after completion.
    /// </summary>
    public enum QuestCategory
    {
        Normal,
        Daily,
        Weekly
    }

    /// <summary>
    /// Authored quest data, loaded from StreamingAssets/Quests/quests.json. Mirrors the
    /// blueprint/item config pattern: pure serializable data, no behaviour. The reward reuses the
    /// same <see cref="RewardTable"/> enemies use, so quest turn-ins flow through RewardGranter.
    /// </summary>
    [Serializable]
    public class QuestDefinition
    {
        public string Id;
        public string Name;
        public string Description;

        // String for the same JsonUtility reason as QuestObjective.Type: "Normal" / "Daily" / "Weekly".
        public string Category = "Normal";
        public List<QuestObjective> Objectives = new List<QuestObjective>();
        public RewardTable Reward = new RewardTable();

        public QuestCategory CategoryValue =>
            Enum.TryParse<QuestCategory>(Category, ignoreCase: true, out var c) ? c : QuestCategory.Normal;
    }
}
