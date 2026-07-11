using System;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Boosters
{
    /// <summary>
    /// Runtime record of one active timed buff, tracked by <see cref="BuffTracker"/> and
    /// visualized by <see cref="BuffIcon"/>. Counts down its remaining time; the tracker
    /// fires its expiry callback and drops it when it reaches zero.
    /// </summary>
    public class ActiveBuff
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public float Duration { get; }
        public float Remaining { get; private set; }

        /// <summary>Remaining time as 0..1 of the original duration (drives a radial fill).</summary>
        public float Normalized => Duration > 0f ? Mathf.Clamp01(Remaining / Duration) : 0f;
        public bool IsExpired => Remaining <= 0f;

        // Runs once when the buff expires (e.g. to remove the stat modifier).
        internal Action OnExpire;

        public ActiveBuff(string id, string displayName, Sprite icon, float duration)
        {
            Id = id;
            DisplayName = displayName;
            Icon = icon;
            Duration = duration;
            Remaining = duration;
        }

        internal void Tick(float deltaTime) => Remaining = Mathf.Max(0f, Remaining - deltaTime);

        /// <summary>Restart the countdown (used when the same buff is re-applied).</summary>
        public void Refresh() => Remaining = Duration;
    }
}
