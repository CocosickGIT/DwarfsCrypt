using UnityEngine;

namespace DwarfsCrypt.Presentation.Skills
{
    /// <summary>
    /// Marks a string field as a skill id. In the inspector it is drawn as a dropdown populated
    /// from the skills config (see the editor-only SkillIdDrawer) instead of a free-text box, so
    /// designers pick a valid skill instead of typing an id by hand.
    /// </summary>
    public class SkillIdAttribute : PropertyAttribute
    {
        /// <summary>Resources path (no extension) of the skills config the dropdown reads.</summary>
        public readonly string ResourcePath;

        public SkillIdAttribute(string resourcePath = "Skills/skills") => ResourcePath = resourcePath;
    }
}
