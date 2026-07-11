using System;
using System.IO;
using DwarfsCrypt.Domain.Skills;
using UnityEngine;

namespace Core.Skills
{
    // Mirrors ItemConfigLoader / CharacterConfigLoader. Prefer LoadFromResources so the
    // config ships inside the build (StreamingAssets is unreadable on packaged Android).
    public static class SkillConfigLoader
    {
        public static SkillsCollection LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Skills config not found: {filePath}");

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<SkillsCollection>(json);
        }

        public static SkillsCollection LoadFromStreamingAssets(string relativePath)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath + ".json");
            return LoadFromFile(fullPath);
        }

        public static SkillsCollection LoadFromResources(string resourcePath)
        {
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException($"Skills config not found in Resources: {resourcePath}");

            return JsonUtility.FromJson<SkillsCollection>(asset.text);
        }
    }
}
