using System;
using Core.Characters;
using Core.Player;
using Core.Rewards;
using Core.Statistics;
using DwarfsCrypt.Domain.Afk;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Rewards;
using UnityEngine;

namespace Core.Afk
{
    /// <summary>
    /// Orchestrates an AFK Farming run: builds the player from the active profile and the mob from
    /// its config, runs the <see cref="AfkRunSimulator"/>, then applies each kill's rewards/statistics
    /// to the profile — exactly like the live kill loop (<see cref="RewardGranter.GrantKill"/> +
    /// <see cref="StatisticsService.RecordKill"/>) but resolved from a simulation instead of real
    /// combat. Static to match the project's other services.
    /// </summary>
    public static class AfkRunService
    {
        /// <summary>AFK runs pay out at half rate — idle farming is worth less than an active run.</summary>
        private const float RewardMultiplier = 0.5f;

        /// <summary>The simulation result plus the loot actually granted, for the summary screen.</summary>
        public class AfkRunOutcome
        {
            public AfkRunResult Result;
            public RewardResult Rewards;
        }

        /// <summary>
        /// Simulate a run against <paramref name="mobCount"/> copies of the mob at
        /// <paramref name="enemyConfigPath"/> (a Resources path, e.g. "Characters/example_enemy"),
        /// bounded by <paramref name="duration"/> seconds. Returns null if the mob config is missing.
        /// </summary>
        public static AfkRunOutcome Run(string enemyConfigPath, int mobCount, float duration)
        {
            if (string.IsNullOrEmpty(enemyConfigPath))
            {
                Debug.LogWarning("[AfkRunService] No enemy config path for this level — AFK run skipped.");
                return null;
            }

            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            var player = new Character(profile.ToCharacterConfig());

            Character enemy;
            try
            {
                enemy = new Character(CharacterConfigLoader.LoadFromResources(enemyConfigPath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[AfkRunService] Could not load mob config '{enemyConfigPath}': {e.Message}");
                return null;
            }

            var result = AfkRunSimulator.Simulate(
                player.Attributes, player.MaxHp,
                enemy.Attributes, enemy.MaxHp,
                mobCount, duration);

            // Grant the spoils for every simulated kill and feed the kill statistics quests read.
            // foldIntoRun:false keeps these out of the live-run total (RunRewards), which is for
            // an actual in-level run, not this town-side simulation.
            var rewards = new RewardResult();
            for (int i = 0; i < result.Kills; i++)
            {
                rewards.Add(RewardGranter.Grant(enemy.Rewards, foldIntoRun: false, rewardScale: RewardMultiplier));
                StatisticsService.RecordKill(enemy.Name);
            }

            if (result.Kills > 0)
                PlayerProfileService.Save();

            return new AfkRunOutcome { Result = result, Rewards = rewards };
        }
    }
}
