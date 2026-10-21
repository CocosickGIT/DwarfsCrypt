using System;
using System.Collections.Generic;
using System.IO;
using DwarfsCrypt.Domain.Characters;
using UnityEngine;

namespace Core.Characters
{
    public static class CharacterConfigLoader
    {
        // Parsed configs are read-only: Character copies the values it needs and never mutates the
        // config (CharacterAttributes only reads Stats), so many enemies can share one cached
        // instance instead of re-parsing the JSON on every spawn.
        private static readonly Dictionary<string, CharacterConfig> _resourceCache = new();
        /// <summary>
        /// Loads a CharacterConfig from an absolute file path.
        /// </summary>
        public static CharacterConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Character config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<CharacterConfig>(json);
        }

        /// <summary>
        /// Loads a CharacterConfig from a path relative to StreamingAssets.
        /// E.g. "Characters/warrior" for StreamingAssets/Characters/warrior.json
        /// </summary>
        public static CharacterConfig LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        /// <summary>
        /// Loads a CharacterConfig from a Resources folder path (no extension).
        /// E.g. "Characters/warrior" for Resources/Characters/warrior.json
        /// </summary>
        public static CharacterConfig LoadFromResources(string resourcePath)
        {
            if (_resourceCache.TryGetValue(resourcePath, out var cached))
                return cached;

            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Character config not found in Resources: {resourcePath}");

            var parsed = JsonUtility.FromJson<CharacterConfig>(asset.text);
            _resourceCache[resourcePath] = parsed;
            return parsed;
        }
    }
}
