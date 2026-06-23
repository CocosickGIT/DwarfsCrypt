using System;
using DwarfsCrypt.Domain.Statistics;

namespace DwarfsCrypt.Domain.Quests
{
    public enum QuestObjectiveType
    {
        Kill,
        Craft
    }

    /// <summary>
    /// A single goal inside a quest, expressed as "reach <see cref="Amount"/> of a tracked statistic".
    /// <see cref="TargetId"/> scopes the statistic: an enemy Name for a Kill objective or a blueprint
    /// Id for a Craft objective. Leave it empty to count any kill/craft.
    ///
    /// <see cref="Type"/> is a string ("Kill" / "Craft") because JsonUtility only round-trips enums as
    /// integers — authored JSON uses strings here, matching CharacterConfig.Race elsewhere.
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string Type;
        public string TargetId;
        public int Amount = 1;

        public QuestObjectiveType ObjectiveType =>
            Enum.TryParse<QuestObjectiveType>(Type, ignoreCase: true, out var t) ? t : QuestObjectiveType.Kill;

        /// <summary>The statistic key this objective tracks, derived from its type and target.</summary>
        public string StatKey()
        {
            switch (ObjectiveType)
            {
                case QuestObjectiveType.Kill:  return StatKeys.Kill(TargetId);
                case QuestObjectiveType.Craft: return StatKeys.Craft(TargetId);
                default:                       return TargetId ?? string.Empty;
            }
        }
    }
}
