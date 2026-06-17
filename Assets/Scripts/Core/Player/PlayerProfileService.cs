using System.IO;
using DwarfsCrypt.Domain.Player;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Owns the active <see cref="PlayerProfile"/> and its persistence.
    ///
    /// Save file : Application.persistentDataPath/player_profile.json  (mutable progress)
    /// Starter   : StreamingAssets/Save/starter_profile.json           (read-only baseline / reset source)
    ///
    /// Static to match the project's other loaders (CharacterConfigLoader, ItemConfigLoader)
    /// and to survive scene loads between Town and the gameplay scenes.
    /// </summary>
    public static class PlayerProfileService
    {
        private const string SaveFileName = "player_profile.json";
        private const string StarterRelativePath = "Save/starter_profile.json";

        public static PlayerProfile Current { get; private set; }

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        private static string StarterPath => Path.Combine(Application.streamingAssetsPath, StarterRelativePath);

        /// <summary>Loads the save if present, otherwise seeds it from the starter profile.</summary>
        public static void Load()
        {
            if (File.Exists(SavePath))
            {
                Current = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(SavePath));
                if (Current != null) return;
                Debug.LogWarning("[PlayerProfileService] Save file was unreadable; reseeding from starter.");
            }

            Current = LoadStarter();
            Save();
        }

        /// <summary>Idempotent guard so the first access auto-loads.</summary>
        public static void EnsureLoaded()
        {
            if (Current == null) Load();
        }

        public static void Save()
        {
            if (Current == null) return;
            File.WriteAllText(SavePath, JsonUtility.ToJson(Current, prettyPrint: true));
        }

        /// <summary>Discards all progress and restores the starter profile (the "reset progress" action).</summary>
        public static void ResetToStarter()
        {
            Current = LoadStarter();
            Save();
        }

        private static PlayerProfile LoadStarter()
        {
            if (!File.Exists(StarterPath))
            {
                Debug.LogError($"[PlayerProfileService] Starter profile not found: {StarterPath}. Using empty profile.");
                return new PlayerProfile();
            }

            return JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(StarterPath)) ?? new PlayerProfile();
        }
    }
}
