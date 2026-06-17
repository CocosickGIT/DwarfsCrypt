using System;
using DwarfsCrypt.Domain.Rewards;

namespace Core.Rewards
{
    /// <summary>
    /// Accumulates everything earned during the current level run so it can be summarized
    /// on the reward screen. Reset when leaving a level. Rewards are also applied to the
    /// player profile immediately on each kill (see <see cref="RewardGranter"/>); this is
    /// purely the running total for display.
    /// </summary>
    public static class RunRewards
    {
        private static RewardResult _summary = new RewardResult();

        public static RewardResult Summary => _summary;

        /// <summary>Raised whenever the run total changes (lets the HUD show live exp/gold).</summary>
        public static event Action OnChanged;

        public static void Add(RewardResult result)
        {
            if (result == null) return;
            _summary.Add(result);
            OnChanged?.Invoke();
        }

        public static void Reset()
        {
            _summary = new RewardResult();
            OnChanged?.Invoke();
        }
    }
}
