using System;
using System.Collections.Generic;
using System.Linq;
using Core.Skills;
using DwarfsCrypt.Domain.Skills;
using DwarfsCrypt.Presentation.Skills;
using UnityEditor;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Skills.EditorTools
{
    /// <summary>
    /// Draws a [SkillId] string field as a dropdown of the ids defined in skills.json, so a skill
    /// is chosen from a list instead of typed. Adds a "(none)" entry for clearing the field, and
    /// keeps any current value that no longer exists in the config visible (flagged "missing") so
    /// it is never silently lost.
    /// </summary>
    [CustomPropertyDrawer(typeof(SkillIdAttribute))]
    public class SkillIdDrawer : PropertyDrawer
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

            string path = ((SkillIdAttribute)attribute).ResourcePath;
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
                SkillsCollection collection = SkillConfigLoader.LoadFromResources(path);
                if (collection?.Skills != null)
                    ids = collection.Skills
                        .Where(s => s != null && !string.IsNullOrEmpty(s.Id))
                        .Select(s => s.Id)
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
