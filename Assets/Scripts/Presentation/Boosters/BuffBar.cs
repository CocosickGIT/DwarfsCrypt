using System.Collections.Generic;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Boosters
{
    /// <summary>
    /// Container for the active-buff icons. Listens to the <see cref="BuffTracker"/> and spawns a
    /// <see cref="BuffIcon"/> per active buff, removing it when the buff expires. Put a layout group
    /// (Horizontal/Grid) on the container so the icons arrange themselves.
    /// </summary>
    public class BuffBar : MonoBehaviour
    {
        [Tooltip("Prefab with a BuffIcon component, instantiated once per active buff.")]
        [SerializeField] private BuffIcon _iconPrefab;
        [Tooltip("Parent the icons spawn under (usually this object with a layout group). Defaults to this transform.")]
        [SerializeField] private Transform _container;

        private BuffTracker _tracker;
        private readonly Dictionary<ActiveBuff, BuffIcon> _icons = new();

        private void Awake()
        {
            if (_container == null) _container = transform;
        }

        private void OnDisable() => Unbind();

        private void Update()
        {
            // The tracker is created lazily when the first buff is applied; bind once it exists.
            if (_tracker == null) TryBind();
        }

        private void TryBind()
        {
            _tracker = BuffTracker.Instance;
            if (_tracker == null) return;

            _tracker.BuffAdded += OnBuffAdded;
            _tracker.BuffRemoved += OnBuffRemoved;

            // Catch up on buffs that were already active before we bound.
            foreach (var buff in _tracker.Active)
                OnBuffAdded(buff);
        }

        private void Unbind()
        {
            if (_tracker != null)
            {
                _tracker.BuffAdded -= OnBuffAdded;
                _tracker.BuffRemoved -= OnBuffRemoved;
                _tracker = null;
            }

            foreach (var icon in _icons.Values)
                if (icon != null) Destroy(icon.gameObject);
            _icons.Clear();
        }

        private void OnBuffAdded(ActiveBuff buff)
        {
            if (_iconPrefab == null || _icons.ContainsKey(buff)) return;

            var icon = Instantiate(_iconPrefab, _container);
            icon.Bind(buff);
            _icons[buff] = icon;
        }

        private void OnBuffRemoved(ActiveBuff buff)
        {
            if (_icons.TryGetValue(buff, out var icon))
            {
                if (icon != null) Destroy(icon.gameObject);
                _icons.Remove(buff);
            }
        }
    }
}
