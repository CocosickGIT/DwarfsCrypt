using System;
using System.Collections.Generic;
using System.IO;
using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace Core.Items
{
    public static class ItemConfigLoader
    {
        // Parsed configs are read-only lookups shared across callers, so cache per path instead of
        // re-running Resources.Load + JsonUtility.FromJson (which allocates) on every call.
        private static readonly Dictionary<string, ItemsCollection> _resourceCache = new();
        public static ItemsCollection LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Items config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<ItemsCollection>(json);
        }

        public static ItemsCollection LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        public static ItemsCollection LoadFromResources(string resourcePath)
        {
            if (_resourceCache.TryGetValue(resourcePath, out var cached))
                return cached;

            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Items config not found in Resources: {resourcePath}");

            var parsed = JsonUtility.FromJson<ItemsCollection>(asset.text);
            _resourceCache[resourcePath] = parsed;
            return parsed;
        }
    }
}
