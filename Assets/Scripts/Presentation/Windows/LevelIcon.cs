using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    /// <summary>
    /// A single selectable level icon in the LevelSelectWindow. Holds the scene this icon
    /// maps to and shows a highlight frame while selected. Clicking it asks the owning window
    /// to make this icon the current selection.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LevelIcon : MonoBehaviour
    {
        [Tooltip("Name of the scene to load when this icon is selected and the player enters. Must match a scene in Build Settings (see SceneNames).")]
        [SerializeField] private string sceneName;

        [Tooltip("Object shown only while this icon is the selected one (e.g. a highlight frame/glow). Optional.")]
        [SerializeField] private GameObject selectedHighlight;

        [Header("AFK Farming")]
        [Tooltip("Resources path (no extension) of the mob this level farms in an AFK run, e.g. " +
                 "'Characters/example_enemy'. Leave empty to hide the AFK Run button for this level.")]
        [SerializeField] private string afkEnemyConfigPath;

        private Button button;
        private LevelSelectWindow owner;

        public string SceneName => sceneName;

        /// <summary>Resources path of the mob simulated in this level's AFK run (empty = AFK disabled).</summary>
        public string AfkEnemyConfigPath => afkEnemyConfigPath;

        /// <summary>True when this level has a mob configured for AFK farming.</summary>
        public bool SupportsAfk => !string.IsNullOrEmpty(afkEnemyConfigPath);

        /// <summary>Called by the window when it builds its icon list. Wires the click into the window.</summary>
        public void Init(LevelSelectWindow window)
        {
            owner = window;
            button = GetComponent<Button>();
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null)
                selectedHighlight.SetActive(selected);
        }

        private void OnClicked()
        {
            owner.Select(this);

            // Drop the EventSystem's focus so the Button's own "Selected" color tint doesn't
            // act as the selection visual. The persistent selectedHighlight frame is the single
            // source of truth and stays on until another icon is clicked.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
