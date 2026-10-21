using System;
using System.Collections.Generic;
using System.Text;
using Core.Towns;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Spawner;
using TMPro;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Towns
{
    /// <summary>
    /// Watches a game scene's spawned enemies and records the zone as cleared once every one of them
    /// is dead — the signal the world map's town unlocks are built on. Drop one on any zone scene
    /// (alongside the CharacterSpawner); it resolves the town/zone it is clearing from
    /// <see cref="RunContext"/>, so entering the same scene from a different town records against
    /// that town instead.
    ///
    /// Polls on an interval rather than subscribing to each enemy's OnDied: enemies stream in over
    /// several frames and are pooled away on death, so a poll over the spawner's live list is both
    /// simpler and immune to units that are removed before their event is seen.
    /// </summary>
    public class ZoneClearTracker : MonoBehaviour
    {
        [Tooltip("Spawner whose enemies count toward the clear. Found in the scene when left empty.")]
        [SerializeField] private CharacterSpawner _spawner;

        [Tooltip("Seconds between checks. The clear is a one-shot end-of-fight event, so this does " +
                 "not need to be per-frame.")]
        [SerializeField, Min(0.05f)] private float _checkInterval = 0.5f;

        [Header("Announcement (optional)")]
        [Tooltip("Object shown when the zone is cleared, e.g. a 'Zone Cleared!' banner. Hidden on load.")]
        [SerializeField] private GameObject _clearedBanner;

        [Tooltip("Label filled with the clear message (and any town it just unlocked).")]
        [SerializeField] private TMP_Text _clearedLabel;

        /// <summary>Raised once when the zone is cleared, with (townId, zoneId).</summary>
        public event Action<string, string> ZoneCleared;

        /// <summary>True once this scene's enemies have all been killed.</summary>
        public bool IsCleared { get; private set; }

        private float _timer;

        private void Awake()
        {
            if (_spawner == null)
                _spawner = FindAnyObjectByType<CharacterSpawner>();

            if (_clearedBanner != null)
                _clearedBanner.SetActive(false);
        }

        private void Update()
        {
            if (IsCleared || _spawner == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = _checkInterval;

            // Enemies spawn over several frames — an empty list before that is "not spawned yet",
            // not "cleared". A scene that spawns none at all never clears.
            if (!_spawner.SpawnComplete || _spawner.TotalEnemiesSpawned == 0) return;
            if (!AllEnemiesDead()) return;

            IsCleared = true;
            RecordClear();
        }

        private bool AllEnemiesDead()
        {
            foreach (var enemy in _spawner.SpawnedEnemies)
            {
                if (enemy == null) continue;

                var character = enemy.GetComponentInChildren<CharacterComponent>();
                // A live unit whose character hasn't initialised yet is not dead — wait for it.
                if (character == null || !character.IsDead) return false;
            }

            return true;
        }

        private void RecordClear()
        {
            string townId = RunContext.TownId;
            string zoneId = RunContext.ZoneId;

            if (string.IsNullOrEmpty(townId) || string.IsNullOrEmpty(zoneId))
            {
                // Playing the scene directly from the editor, or entered by something that didn't
                // name a zone. Nothing to record against — still announce the clear.
                Debug.LogWarning("[ZoneClearTracker] Zone cleared but no town/zone is set on " +
                                 "RunContext; progress was not recorded. (Entered outside the map?)");
                Announce("Zone Cleared!");
                ZoneCleared?.Invoke(townId, zoneId);
                return;
            }

            bool firstClear = TownProgressService.MarkZoneCleared(townId, zoneId);
            var unlocked = firstClear ? TownProgressService.TownsUnlockedBy(townId) : new List<string>();

            Announce(BuildMessage(unlocked));
            ZoneCleared?.Invoke(townId, zoneId);
        }

        private static string BuildMessage(List<string> unlockedTownIds)
        {
            var message = new StringBuilder("Zone Cleared!");

            foreach (var id in unlockedTownIds)
            {
                if (!TownCatalog.TryGet(id, out var town)) continue;
                message.Append("\nNew town unlocked: ").Append(town.Name);
            }

            return message.ToString();
        }

        private void Announce(string message)
        {
            Debug.Log($"[ZoneClearTracker] {message.Replace("\n", " ")}");

            if (_clearedLabel != null)
                _clearedLabel.text = message;

            if (_clearedBanner != null)
                _clearedBanner.SetActive(true);
        }
    }
}
