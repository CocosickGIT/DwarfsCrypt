using System;
using System.IO;
using DwarfsCrypt.Domain.Towns;
using UnityEngine;

namespace Core.Towns
{
    public static class TownConfigLoader
    {
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
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Towns config not found in Resources: {resourcePath}");

            return JsonUtility.FromJson<TownsCollection>(asset.text);
        }
    }
}
