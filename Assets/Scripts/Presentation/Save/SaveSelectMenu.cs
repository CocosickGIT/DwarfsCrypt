using UnityEngine;
using Core.Player;
using Core.Towns;

namespace DwarfsCrypt.Presentation.Save
{
    /// <summary>
    /// InitScene entry point. Lists the save slots and, when one is chosen, loads (or starts) that
    /// profile and enters the gameplay scene. Spawns one <see cref="SaveSlotButton"/> per slot under
    /// a container (give it a Vertical Layout Group), mirroring the buff-bar pattern.
    /// </summary>
    public class SaveSelectMenu : MonoBehaviour
    {
        [Tooltip("Prefab with a SaveSlotButton component, instantiated once per slot.")]
        [SerializeField] private SaveSlotButton _slotButtonPrefab;
        [Tooltip("Parent the slot buttons spawn under (defaults to this transform).")]
        [SerializeField] private Transform _slotContainer;
        [Tooltip("Optional override for the town scene loaded after a slot is chosen. Leave empty " +
                 "to enter the town the slot was last saved in (the starting town for a new game).")]
        [SerializeField] private string _sceneToLoad;

        private void Awake()
        {
            if (_slotContainer == null) _slotContainer = transform;
        }

        private void Start() => Refresh();

        public void Refresh()
        {
            for (int i = _slotContainer.childCount - 1; i >= 0; i--)
                Destroy(_slotContainer.GetChild(i).gameObject);

            if (_slotButtonPrefab == null)
            {
                Debug.LogWarning("[SaveSelectMenu] No slot button prefab assigned.");
                return;
            }

            foreach (var info in PlayerProfileService.GetSlots())
            {
                var button = Instantiate(_slotButtonPrefab, _slotContainer);
                button.Bind(info, OnSlotSelected, OnSlotDeleted);
            }
        }

        private void OnSlotSelected(int slot)
        {
            // Loads the slot (or seeds a new game from the starter profile), then enters the town.
            PlayerProfileService.SelectSlot(slot);

            // Resume in the town this slot was last in — a new game (or a save pointing at a town
            // that no longer exists) starts in the default one. The hub builds itself from this.
            TownContext.RestoreFromProfile();

            string townScene = string.IsNullOrEmpty(_sceneToLoad)
                ? TownCatalog.SceneFor(TownContext.CurrentTownId)
                : _sceneToLoad;
            Core.Scenes.SceneLoader.Load(townScene);
        }

        private void OnSlotDeleted(int slot)
        {
            PlayerProfileService.DeleteSlot(slot);
            Refresh();
        }
    }
}
