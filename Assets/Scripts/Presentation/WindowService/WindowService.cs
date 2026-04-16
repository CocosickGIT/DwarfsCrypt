using System.Collections.Generic;
using DwarfsCrypt.Presentation.Windows;
using UnityEngine;

namespace Presentation.WindowService
{
    public class WindowService : MonoBehaviour, IWindowService
    {
        [SerializeField] private List<WindowBase> _windows;

        private readonly Dictionary<WindowType, WindowBase> _windowMap = new();

        private void Awake()
        {
            foreach (var window in _windows)
            {
                _windowMap[window.Type] = window;
                window.Close();
            }
        }

        public void Open(WindowType type)
        {
            if (_windowMap.TryGetValue(type, out var window))
                window.Open();
            else
                Debug.LogWarning($"[WindowService] Window not registered: {type}");
        }

        public void Close(WindowType type)
        {
            if (_windowMap.TryGetValue(type, out var window))
                window.Close();
        }

        public void CloseAll()
        {
            foreach (var window in _windowMap.Values)
                window.Close();
        }

        public bool IsOpen(WindowType type)
        {
            return _windowMap.TryGetValue(type, out var window) && window.IsOpen;
        }

        public T GetWindow<T>(WindowType type) where T : WindowBase
        {
            if (_windowMap.TryGetValue(type, out var window))
                return window as T;

            Debug.LogWarning($"[WindowService] Window not registered: {type}");
            return null;
        }
    }
}
