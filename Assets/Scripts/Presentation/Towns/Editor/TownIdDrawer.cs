using System;
using System.Collections.Generic;
using System.Linq;
using Core.Towns;
using DwarfsCrypt.Domain.Towns;
using DwarfsCrypt.Presentation.Towns;
using UnityEditor;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Towns.EditorTools
{
    /// <summary>
    /// Draws a [TownId] string field as a dropdown of the ids defined in towns.json, so a town
    /// is chosen from a list instead of typed. Adds a "(none)" entry for clearing the field, and
    /// keeps any current value that no longer exists in the config visible (flagged "missing") so
    /// it is never silently lost. Mirrors <see cref="Skills.EditorTools.SkillIdDrawer"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TownIdAttribute))]
    public class TownIdDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(none)";

        // Cache the id list per config path so we don't re-read the asset on every repaint.
        private static readonly Dictionary<string, string[]> _idCache = new();
        private static double _nextRefreshAt;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            string path = ((TownIdAttribute)attribute).ResourcePath;
            string[] ids = GetIds(path);

            // Build the option list: "(none)" + every id, plus the current value if it's gone missing.
            var options = new List<string> { NoneLabel };
            options.AddRange(ids);

            string current = property.stringValue;
            bool missing = !string.IsNullOrEmpty(current) && !ids.Contains(current);
            if (missing) options.Add($"{current} (missing)");

            int currentIndex =
                string.IsNullOrEmpty(current) ? 0 :
                missing ? options.Count - 1 :
                options.IndexOf(current);

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());
            if (newIndex == currentIndex) return;

            property.stringValue = newIndex == 0 ? string.Empty : ids[newIndex - 1];
        }

        private static string[] GetIds(string path)
        {
            // Refresh at most a few times a second, and always when the cache is empty.
            if (_idCache.TryGetValue(path, out string[] cached) && EditorApplication.timeSinceStartup < _nextRefreshAt)
                return cached;

            string[] ids = Array.Empty<string>();
            try
            {
                TownsCollection collection = TownConfigLoader.LoadFromResources(path);
                if (collection?.Towns != null)
                    ids = collection.Towns
                        .Where(t => t != null && !string.IsNullOrEmpty(t.Id))
                        .Select(t => t.Id)
                        .ToArray();
            }
            catch (Exception)
            {
                // Config missing/unparseable in the editor — fall back to an empty list; the field
                // still works via the "(none)"/"missing" entries.
            }

            _idCache[path] = ids;
            _nextRefreshAt = EditorApplication.timeSinceStartup + 0.5;
            return ids;
        }
    }
}
