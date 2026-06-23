using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Quests
{
    /// <summary>JSON root for quests.json, mirroring BlueprintCollection.</summary>
    [Serializable]
    public class QuestCollection
    {
        public List<QuestDefinition> Quests = new List<QuestDefinition>();
    }
}
