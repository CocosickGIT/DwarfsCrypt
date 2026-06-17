using System.Collections.Generic;
using Core.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class LevelSelectWindow : WindowBase
    {
        [Tooltip("All level icons, in display order. Icon 0 -> its scene, icon 1 -> its scene, etc.")]
        [SerializeField] private List<LevelIcon> icons = new List<LevelIcon>();

        [Tooltip("Button that enters the selected level. Stays disabled until a level is selected.")]
        [SerializeField] private Button enterButton;

        private LevelIcon selectedIcon;

        public override WindowType Type => WindowType.LevelSelect;

        private void Awake()
        {
            foreach (var icon in icons)
                icon.Init(this);
        }

        protected override void OnOpen()
        {
            // Start with no selection; the player must pick a level before the Enter button works.
            ClearSelection();
        }

        /// <summary>Called by an icon when it is clicked. Highlights it and remembers it as the target.</summary>
        public void Select(LevelIcon icon)
        {
            if (selectedIcon != null)
                selectedIcon.SetSelected(false);

            selectedIcon = icon;
            selectedIcon.SetSelected(true);

            if (enterButton != null)
                enterButton.gameObject.SetActive(true);
        }

        private void ClearSelection()
        {
            if (selectedIcon != null)
                selectedIcon.SetSelected(false);

            selectedIcon = null;

            if (enterButton != null)
                enterButton.gameObject.SetActive(false);
        }

        /// <summary>Wired to the EnterButton's onClick. Loads the scene bound to the selected icon.</summary>
        public void EnterSelectedZone()
        {
            if (selectedIcon == null)
            {
                Debug.LogWarning("[LevelSelectWindow] Enter pressed with no icon selected.");
                return;
            }

            SceneLoader.Load(selectedIcon.SceneName);
        }
    }
}
