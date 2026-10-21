using UnityEngine;

namespace DwarfsCrypt.Presentation.Towns
{
    /// <summary>
    /// Marks a string field as a town id. In the inspector it is drawn as a dropdown populated
    /// from the towns config (see the editor-only TownIdDrawer) instead of a free-text box, so
    /// designers pick a valid town instead of typing an id by hand. Mirrors
    /// <see cref="Skills.SkillIdAttribute"/>.
    /// </summary>
    public class TownIdAttribute : PropertyAttribute
    {
        /// <summary>Resources path (no extension) of the towns config the dropdown reads.</summary>
        public readonly string ResourcePath;

        public TownIdAttribute(string resourcePath = "Towns/towns") => ResourcePath = resourcePath;
    }
}
