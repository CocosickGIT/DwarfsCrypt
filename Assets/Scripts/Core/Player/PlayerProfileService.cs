using System.Collections.Generic;
using System.IO;
using DwarfsCrypt.Domain.Player;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Owns the active <see cref="PlayerProfile"/> and its persistence across multiple save slots.
    ///
    /// Save files : Application.persistentDataPath/Saves/save_{slot}.json  (mutable progress, one per slot)
    /// Starter    : Resources/Save/starter_profile.json                    (read-only baseline / new-game source)
    ///
    /// The InitScene save-select menu picks a slot via <see cref="SelectSlot"/> before loading the
    /// gameplay scene. Static so the chosen profile survives scene loads (Init → Town → Game).
    /// </summary>
    public static class PlayerProfileService
    {
        /// <summary>Number of save slots offered in the select screen.</summary>
        public const int SlotCount = 3;

        private const int DefaultSlot = 0;
        private const string SavesFolder = "Saves";
        // Resources-relative path (no extension). Loaded via Resources.Load so it works in
        // packaged builds (Android/iOS/WebGL) where StreamingAssets is not a readable file path.
        private const string StarterResourcePath = "Save/starter_profile";

        /// <summary>The loaded profile for the active slot, or null before a slot is selected.</summary>
        public static PlayerProfile Current { get; private set; }

        /// <summary>Index of the slot <see cref="Current"/> belongs to, or -1 if none is active.</summary>
        public static int ActiveSlot { get; private set; } = -1;

        private static string SavesDir => Path.Combine(Application.persistentDataPath, SavesFolder);
        private static string SlotPath(int slot) => Path.Combine(SavesDir, $"save_{slot}.json");

        public static bool SlotExists(int slot) => File.Exists(SlotPath(slot));

        /// <summary>
        /// Guarantees a profile is loaded. If a slot was already chosen (e.g. via the InitScene menu)
        /// this is a no-op; otherwise it falls back to the default slot so launching a gameplay scene
        /// directly (without going through InitScene) still spawns a valid player.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (Current != null) return;
            SelectSlot(ActiveSlot >= 0 ? ActiveSlot : DefaultSlot);
        }

        /// <summary>
        /// Make <paramref name="slot"/> the active slot and load its profile, seeding it from the
        /// starter profile (a "New Game") when the slot is empty. Called by the save-select menu.
        /// </summary>
        public static void SelectSlot(int slot)
        {
            ActiveSlot = slot;
            Current = LoadSlot(slot) ?? LoadStarter();
            Save();
        }

        public static void Save()
        {
            if (Current == null || ActiveSlot < 0) return;
            Directory.CreateDirectory(SavesDir);
            File.WriteAllText(SlotPath(ActiveSlot), JsonUtility.ToJson(Current, prettyPrint: true));
        }

        /// <summary>Discards the active slot's progress and restores the starter profile.</summary>
        public static void ResetToStarter()
        {
            Current = LoadStarter();
            Save();
        }

        /// <summary>Delete a slot's save file. Clears the active profile if that slot was loaded.</summary>
        public static void DeleteSlot(int slot)
        {
            string path = SlotPath(slot);
            if (File.Exists(path)) File.Delete(path);

            if (slot == ActiveSlot)
            {
                Current = null;
                ActiveSlot = -1;
            }
        }

        /// <summary>Summaries of every slot for the save-select UI (does not change the active slot).</summary>
        public static List<SaveSlotInfo> GetSlots()
        {
            var slots = new List<SaveSlotInfo>(SlotCount);
            for (int i = 0; i < SlotCount; i++)
                slots.Add(GetSlotInfo(i));
            return slots;
        }

        public static SaveSlotInfo GetSlotInfo(int slot)
        {
            var profile = LoadSlot(slot);
            if (profile == null) return SaveSlotInfo.Empty(slot);

            return new SaveSlotInfo
            {
                Slot = slot,
                Exists = true,
                Name = profile.Name,
                Race = profile.Race,
                Level = profile.Level,
                Gold = profile.Gold
            };
        }

        private static PlayerProfile LoadSlot(int slot)
        {
            string path = SlotPath(slot);
            if (!File.Exists(path)) return null;

            var profile = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(path));
            if (profile == null)
                Debug.LogWarning($"[PlayerProfileService] Save in slot {slot} was unreadable.");
            return profile;
        }

        private static PlayerProfile LoadStarter()
        {
            var asset = Resources.Load<TextAsset>(StarterResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[PlayerProfileService] Starter profile not found in Resources: {StarterResourcePath}. Using empty profile.");
                return new PlayerProfile();
            }

            return JsonUtility.FromJson<PlayerProfile>(asset.text) ?? new PlayerProfile();
        }
    }
}
