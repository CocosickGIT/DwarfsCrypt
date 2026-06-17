using System;
using System.IO;
using DwarfsCrypt.Domain.Crafting;
using UnityEngine;

namespace Core.Crafting
{
    /// <summary>
    /// Loads <see cref="BlueprintCollection"/> JSON the same way <c>ItemConfigLoader</c> loads items.
    /// </summary>
    public static class BlueprintConfigLoader
    {
        public static BlueprintCollection LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Blueprints config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<BlueprintCollection>(json);
        }

        public static BlueprintCollection LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        public static BlueprintCollection LoadFromResources(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Blueprints config not found in Resources: {resourcePath}");

            return JsonUtility.FromJson<BlueprintCollection>(asset.text);
        }
    }
}
