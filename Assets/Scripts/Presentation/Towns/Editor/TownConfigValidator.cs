using System.Collections.Generic;
using System.IO;
using System.Text;
using DwarfsCrypt.Domain.Crafting;
using DwarfsCrypt.Domain.Quests;
using DwarfsCrypt.Domain.Towns;
using UnityEditor;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Towns.EditorTools
{
    /// <summary>
    /// Checks every string key the town system is wired with — scene names, Resources paths, town ids
    /// on blueprints and quests — and reports the ones that don't resolve.
    ///
    /// Worth having because all of these fail quietly: a mistyped town id in a blueprint's Towns list
    /// just makes the recipe vanish from every forge, and a zone Scene missing from Build Settings
    /// only shows up when a player presses Enter on it. Run it from
    /// <c>Tools > DwarfsCrypt > Validate Town Config</c> after editing the json.
    ///
    /// Parses the assets directly rather than going through TownCatalog/TownConfigLoader, both of
    /// which cache statically — re-running after an edit would otherwise re-check the old data.
    /// </summary>
    public static class TownConfigValidator
    {
        private const string TownsPath = "Towns/towns";
        private const string BlueprintsPath = "Crafting/blueprints";
        private const string QuestsPath = "Quests/quests";

        [MenuItem("Tools/DwarfsCrypt/Validate Town Config")]
        public static void Validate()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            var towns = Parse<TownsCollection>(TownsPath)?.Towns;
            if (towns == null || towns.Count == 0)
            {
                Debug.LogError($"[TownConfigValidator] No towns found in Resources/{TownsPath}.json.");
                return;
            }

            var sceneNames = BuildSettingsSceneNames();
            var townIds = new HashSet<string>();

            foreach (var town in towns)
            {
                if (town == null) continue;

                string label = string.IsNullOrEmpty(town.Id) ? "(town with no Id)" : town.Id;

                if (string.IsNullOrEmpty(town.Id))
                    errors.Add("A town has no Id and will be skipped by TownCatalog.");
                else if (!townIds.Add(town.Id))
                    errors.Add($"Duplicate town id '{town.Id}' — the later one wins in TownCatalog.");

                ValidateTown(town, label, sceneNames, errors, warnings);
            }

            ValidateUnlockGraph(towns, townIds, errors, warnings);

            foreach (var town in towns)
                if (town?.Zones != null)
                    ValidateZones(town, sceneNames, errors, warnings);

            ValidateTownTags(townIds, errors);

            Report(errors, warnings, towns.Count);
        }

        // --- Towns ---

        private static void ValidateTown(TownConfig town, string label, HashSet<string> sceneNames,
                                         List<string> errors, List<string> warnings)
        {
            if (string.IsNullOrEmpty(town.Scene))
                errors.Add($"{label}: no Scene — there is no hub to load or return to.");
            else if (!sceneNames.Contains(town.Scene))
                errors.Add($"{label}: hub Scene '{town.Scene}' is not in Build Settings.");

            if (string.IsNullOrEmpty(town.EnvironmentPrefab))
                warnings.Add($"{label}: no EnvironmentPrefab — the hub will build as a bare scene.");
            else if (Resources.Load<GameObject>(town.EnvironmentPrefab) == null)
                errors.Add($"{label}: EnvironmentPrefab '{town.EnvironmentPrefab}' does not resolve " +
                           "under a Resources folder.");

            if (town.MapPosition != null &&
                (town.MapPosition.X < 0f || town.MapPosition.X > 1f ||
                 town.MapPosition.Y < 0f || town.MapPosition.Y > 1f))
                warnings.Add($"{label}: MapPosition ({town.MapPosition.X}, {town.MapPosition.Y}) is " +
                             "outside 0..1 and will place the node off the map.");

            if (town.Zones == null || town.Zones.Count == 0)
                warnings.Add($"{label}: no Zones — the gate will be empty and the town can never be " +
                             "cleared, so anything it gates stays locked forever.");
        }

        private static void ValidateUnlockGraph(List<TownConfig> towns, HashSet<string> townIds,
                                                List<string> errors, List<string> warnings)
        {
            bool anyStarting = false;

            foreach (var town in towns)
            {
                if (town == null || string.IsNullOrEmpty(town.Id)) continue;

                if (string.IsNullOrEmpty(town.UnlockedBy)) { anyStarting = true; continue; }

                if (!townIds.Contains(town.UnlockedBy))
                    errors.Add($"{town.Id}: UnlockedBy names unknown town '{town.UnlockedBy}' — this " +
                               "town can never unlock.");
                else if (town.UnlockedBy == town.Id)
                    errors.Add($"{town.Id}: UnlockedBy points at itself — this town can never unlock.");
            }

            if (!anyStarting)
                errors.Add("No town has an empty UnlockedBy, so no town is open at the start.");
        }

        // --- Zones ---

        private static void ValidateZones(TownConfig town, HashSet<string> sceneNames,
                                          List<string> errors, List<string> warnings)
        {
            var zoneIds = new HashSet<string>();

            foreach (var zone in town.Zones)
            {
                if (zone == null) continue;

                string label = $"{town.Id}/{(string.IsNullOrEmpty(zone.Id) ? "(zone with no Id)" : zone.Id)}";

                if (string.IsNullOrEmpty(zone.Id))
                    errors.Add($"{label}: no Id — clearing it can never count toward a town unlock.");
                else if (!zoneIds.Add(zone.Id))
                    errors.Add($"{label}: duplicate zone id within this town.");

                if (string.IsNullOrEmpty(zone.Scene))
                    errors.Add($"{label}: no Scene — the gate skips this zone entirely.");
                else if (!sceneNames.Contains(zone.Scene))
                    errors.Add($"{label}: Scene '{zone.Scene}' is not in Build Settings.");

                if (!string.IsNullOrEmpty(zone.AfkEnemyConfigPath) &&
                    Resources.Load<TextAsset>(zone.AfkEnemyConfigPath) == null)
                    errors.Add($"{label}: AfkEnemyConfigPath '{zone.AfkEnemyConfigPath}' does not " +
                               "resolve under a Resources folder.");

                ValidateMonsterTable(zone, label, errors, warnings);
            }
        }

        private static void ValidateMonsterTable(TownZone zone, string label,
                                                 List<string> errors, List<string> warnings)
        {
            if (zone.MonsterTable == null || zone.MonsterTable.Count == 0)
            {
                warnings.Add($"{label}: no MonsterTable — this zone spawns whatever its scene's camps " +
                             "were authored with, the same as every other town using that scene.");
                return;
            }

            foreach (var mob in zone.MonsterTable)
            {
                if (mob == null) continue;

                if (string.IsNullOrEmpty(mob.MonsterId))
                    errors.Add($"{label}: a MonsterTable entry has no MonsterId and cannot resolve to a prefab.");

                if (mob.Weight <= 0f)
                    warnings.Add($"{label}: '{mob.MonsterId}' has Weight {mob.Weight} and will never spawn.");

                if (!string.IsNullOrEmpty(mob.ConfigPath) &&
                    Resources.Load<TextAsset>(mob.ConfigPath) == null)
                    errors.Add($"{label}: '{mob.MonsterId}' ConfigPath '{mob.ConfigPath}' does not " +
                               "resolve under a Resources folder.");
            }
        }

        // --- Town tags on content ---

        private static void ValidateTownTags(HashSet<string> townIds, List<string> errors)
        {
            var blueprints = Parse<BlueprintCollection>(BlueprintsPath)?.Blueprints;
            if (blueprints != null)
                foreach (var bp in blueprints)
                    if (bp != null)
                        CheckTags(bp.Towns, townIds, $"Blueprint '{bp.Id}'", errors);

            var quests = Parse<QuestCollection>(QuestsPath)?.Quests;
            if (quests != null)
                foreach (var quest in quests)
                    if (quest != null)
                        CheckTags(quest.Towns, townIds, $"Quest '{quest.Id}'", errors);
        }

        private static void CheckTags(List<string> towns, HashSet<string> townIds, string label,
                                      List<string> errors)
        {
            if (towns == null) return;

            foreach (var id in towns)
                if (!townIds.Contains(id))
                    errors.Add($"{label}: Towns lists unknown town '{id}' — it will be offered nowhere " +
                               "that id is expected.");
        }

        // --- Plumbing ---

        private static T Parse<T>(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[TownConfigValidator] Not found in Resources: {resourcePath}");
                return default;
            }

            return JsonUtility.FromJson<T>(asset.text);
        }

        private static HashSet<string> BuildSettingsSceneNames()
        {
            var names = new HashSet<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene != null && scene.enabled)
                    names.Add(Path.GetFileNameWithoutExtension(scene.path));

            return names;
        }

        private static void Report(List<string> errors, List<string> warnings, int townCount)
        {
            if (warnings.Count > 0)
                Debug.LogWarning(Join($"[TownConfigValidator] {warnings.Count} warning(s):", warnings));

            if (errors.Count > 0)
            {
                Debug.LogError(Join($"[TownConfigValidator] {errors.Count} error(s):", errors));
                return;
            }

            Debug.Log($"[TownConfigValidator] {townCount} town(s) validated, no errors" +
                      (warnings.Count > 0 ? $" ({warnings.Count} warning(s) above)." : "."));
        }

        private static string Join(string header, List<string> lines)
        {
            var sb = new StringBuilder(header);
            foreach (var line in lines)
                sb.Append("\n  • ").Append(line);

            return sb.ToString();
        }
    }
}
