using System;
using System.Collections.Generic;
using System.IO;
using DwarfsCrypt.Domain.Towns;
using UnityEngine;

namespace Core.Towns
{
    public static class TownConfigLoader
    {
        // Parsed configs are read-only lookups shared across callers, so cache per path instead of
        // re-running Resources.Load + JsonUtility.FromJson (which allocates) on every call.
        private static readonly Dictionary<string, TownsCollection> _resourceCache = new();
        public static TownsCollection LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Towns config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<TownsCollection>(json);
        }

        public static TownsCollection LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        public static TownsCollection LoadFromResources(string resourcePath)
        {
            if (_resourceCache.TryGetValue(resourcePath, out var cached))
                return cached;

            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Towns config not found in Resources: {resourcePath}");

            var parsed = JsonUtility.FromJson<TownsCollection>(asset.text);
            _resourceCache[resourcePath] = parsed;
            return parsed;
        }
    }
}
