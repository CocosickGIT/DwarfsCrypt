using System;
using System.IO;
using DwarfsCrypt.Domain.Items;
using UnityEngine;

namespace Core.Items
{
    public static class ItemConfigLoader
    {
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
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Items config not found in Resources: {resourcePath}");

            return JsonUtility.FromJson<ItemsCollection>(asset.text);
        }
    }
}
