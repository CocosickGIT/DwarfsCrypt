using System.Collections.Generic;
using DwarfsCrypt.Domain.Quests;

namespace Core.Quests
{
    /// <summary>
    /// In-memory list of every quest definition, loaded once from
    /// Resources/Quests/quests.json. Mirrors <c>BlueprintCatalog</c>.
    /// </summary>
    public static class QuestCatalog
    {
        private const string QuestsPath = "Quests/quests";

        private static List<QuestDefinition> _all;
        private static Dictionary<string, QuestDefinition> _byId;

        public static IReadOnlyList<QuestDefinition> All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        public static void EnsureLoaded()
        {
            if (_all != null) return;

            _all = new List<QuestDefinition>();
            _byId = new Dictionary<string, QuestDefinition>();

            var collection = QuestConfigLoader.LoadFromResources(QuestsPath);
            if (collection?.Quests == null) return;

            foreach (var quest in collection.Quests)
            {
                if (quest == null || string.IsNullOrEmpty(quest.Id)) continue;
                _all.Add(quest);
                _byId[quest.Id] = quest;
            }
        }

        public static bool TryGet(string id, out QuestDefinition quest)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out quest);
        }

        public static QuestDefinition Get(string id) => TryGet(id, out var quest) ? quest : null;
    }
}
