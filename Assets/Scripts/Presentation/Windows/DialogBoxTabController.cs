using System;
using UnityEngine;
using UnityEngine.UI;

namespace DwarfsCrypt.Presentation.Windows
{
    public class DialogBoxTabController : MonoBehaviour
    {
        [Serializable]
        private struct Tab
        {
            [SerializeField] internal Button button;
            [Tooltip("GameObject shown while this tab is active and hidden otherwise.")]
            [SerializeField] internal GameObject target;
        }

        [Header("Tabs")]
        [SerializeField] private Tab[] _tabs;

        private void Awake()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                var tab = _tabs[i];
                if (tab.button != null)
                    tab.button.onClick.AddListener(() => ShowOnly(tab.target));
            }
        }

        private void Start()
        {
            HideAll();
        }

        private void ShowOnly(GameObject target)
        {
            foreach (var tab in _tabs)
            {
                if (tab.target != null)
                    tab.target.SetActive(tab.target == target);
            }
        }

        private void HideAll()
        {
            foreach (var tab in _tabs)
            {
                if (tab.target != null)
                    tab.target.SetActive(false);
            }
        }
    }
}
