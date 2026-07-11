using System;
using System.Collections.Generic;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Boosters
{
    /// <summary>
    /// Scene-scoped registry of active timed buffs. Owns the countdown for every buff, runs the
    /// expiry callback (e.g. removing the stat modifier) when one runs out, and raises add/remove
    /// events the buff UI listens to. Re-applying the same id refreshes its timer instead of
    /// stacking a second icon.
    /// </summary>
    public class BuffTracker : MonoBehaviour
    {
        private static BuffTracker _instance;

        /// <summary>The active tracker, auto-created if none was placed in the scene.</summary>
        public static BuffTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BuffTracker>();
                    if (_instance == null)
                        _instance = new GameObject(nameof(BuffTracker)).AddComponent<BuffTracker>();
                }
                return _instance;
            }
        }

        private readonly List<ActiveBuff> _buffs = new();
        public IReadOnlyList<ActiveBuff> Active => _buffs;

        public event Action<ActiveBuff> BuffAdded;
        public event Action<ActiveBuff> BuffRemoved;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Register a buff for the given duration. If a buff with the same id is already active,
        /// its timer is refreshed (no duplicate icon) and the existing instance is returned.
        /// <paramref name="onExpire"/> runs once when the timer reaches zero.
        /// </summary>
        public ActiveBuff Add(string id, string displayName, Sprite icon, float duration, Action onExpire = null)
        {
            var existing = _buffs.Find(b => b.Id == id);
            if (existing != null)
            {
                existing.Refresh();
                return existing;
            }

            var buff = new ActiveBuff(id, displayName, icon, duration) { OnExpire = onExpire };
            _buffs.Add(buff);
            BuffAdded?.Invoke(buff);
            return buff;
        }

        private void Update()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.Tick(Time.deltaTime);
                if (!buff.IsExpired) continue;

                _buffs.RemoveAt(i);
                buff.OnExpire?.Invoke();
                BuffRemoved?.Invoke(buff);
            }
        }
    }
}
