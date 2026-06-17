using System.Collections.Generic;
using DwarfsCrypt.Domain.Items;

namespace Core.Items
{
    /// <summary>
    /// In-memory lookup of every item by id, loaded once from StreamingAssets/Items/items.json.
    /// Resolves drop ids and saved inventory ids back to full <see cref="ItemData"/>.
    /// </summary>
    public static class ItemCatalog
    {
        private const string ItemsPath = "Items/items";

        private static Dictionary<string, ItemData> _byId;

        public static void EnsureLoaded()
        {
            if (_byId != null) return;

            _byId = new Dictionary<string, ItemData>();
            var collection = ItemConfigLoader.LoadFromStreamingAssets(ItemsPath);
            if (collection?.Items == null) return;

            foreach (var item in collection.Items)
            {
                if (item != null && !string.IsNullOrEmpty(item.Id))
                    _byId[item.Id] = item;
            }
        }

        public static bool TryGet(string id, out ItemData item)
        {
            EnsureLoaded();
            return _byId.TryGetValue(id, out item);
        }

        public static ItemData Get(string id)
        {
            return TryGet(id, out var item) ? item : null;
        }
    }
}
