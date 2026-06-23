using System;
using System.IO;
using DwarfsCrypt.Domain.Quests;
using UnityEngine;

namespace Core.Quests
{
    /// <summary>
    /// Loads <see cref="QuestCollection"/> JSON, mirroring BlueprintConfigLoader / ItemConfigLoader.
    /// </summary>
    public static class QuestConfigLoader
    {
        public static QuestCollection LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Quests config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<QuestCollection>(json);
        }

        public static QuestCollection LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        public static QuestCollection LoadFromResources(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Quests config not found in Resources: {resourcePath}");

            return JsonUtility.FromJson<QuestCollection>(asset.text);
        }
    }
}
