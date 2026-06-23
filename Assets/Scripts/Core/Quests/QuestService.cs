using System;
using System.Collections.Generic;
using Core.Player;
using Core.Rewards;
using Core.Statistics;
using DwarfsCrypt.Domain.Player;
using DwarfsCrypt.Domain.Quests;

namespace Core.Quests
{
    /// <summary>
    /// Quest lifecycle on the active <see cref="PlayerProfile"/>: which quests a giver can offer,
    /// accepting (snapshotting a statistic baseline), evaluating progress, and claiming rewards.
    ///
    /// Progress model: each accepted objective stores the statistic value at accept-time as a
    /// <c>Baseline</c>. Live progress is <c>currentStat - Baseline</c> clamped to [0, Required], so a
    /// fresh "kill 100" quest starts at 0 even when the lifetime kill count is already 200 — and then
    /// counts the next 100 kills on top of it. Stateless/static to match RewardGranter & CraftingService.
    /// </summary>
    public static class QuestService
    {
        // --- Queries ---

        /// <summary>Quests the giver can offer right now: not currently active, and off cooldown.</summary>
        public static List<QuestDefinition> GetAvailable()
        {
            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            var available = new List<QuestDefinition>();
            foreach (var def in QuestCatalog.All)
            {
                if (def == null) continue;
                if (IsAvailable(profile, def))
                    available.Add(def);
            }
            return available;
        }

        /// <summary>Accepted quests that are still Active or waiting to be claimed.</summary>
        public static List<QuestProgress> GetActive()
        {
            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            Evaluate(profile);

            var active = new List<QuestProgress>();
            if (profile.QuestLog == null) return active;

            foreach (var progress in profile.QuestLog)
                if (progress != null &&
                    (progress.State == QuestState.Active || progress.State == QuestState.ReadyToClaim))
                    active.Add(progress);

            return active;
        }

        public static QuestProgress GetProgress(string questId)
        {
            PlayerProfileService.EnsureLoaded();
            return FindEntry(PlayerProfileService.Current, questId);
        }

        /// <summary>Live count for an objective: how much of its requirement is currently met.</summary>
        public static int CurrentAmount(QuestObjectiveProgress objective)
        {
            if (objective == null) return 0;
            int delta = StatisticsService.Get(objective.StatKey) - objective.Baseline;
            return Clamp(delta, 0, objective.Required);
        }

        // --- Commands ---

        /// <summary>
        /// Take a quest from the giver. Snapshots the current statistic for each objective as its
        /// baseline and (re)creates the quest-log entry. No-op if the quest isn't available.
        /// </summary>
        public static bool Accept(string questId)
        {
            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            if (!QuestCatalog.TryGet(questId, out var def)) return false;
            if (!IsAvailable(profile, def)) return false;

            var progress = new QuestProgress
            {
                QuestId = def.Id,
                State = QuestState.Active,
                AcceptedAtTicks = DateTime.UtcNow.Ticks,
                Objectives = new List<QuestObjectiveProgress>()
            };

            if (def.Objectives != null)
            {
                foreach (var objective in def.Objectives)
                {
                    if (objective == null) continue;
                    string key = objective.StatKey();
                    progress.Objectives.Add(new QuestObjectiveProgress
                    {
                        StatKey = key,
                        Baseline = StatisticsService.Get(profile, key),
                        Required = Math.Max(1, objective.Amount)
                    });
                }
            }

            profile.QuestLog ??= new List<QuestProgress>();
            profile.QuestLog.RemoveAll(p => p != null && p.QuestId == def.Id);
            profile.QuestLog.Add(progress);

            PlayerProfileService.Save();
            return true;
        }

        /// <summary>
        /// Grant a ready quest's reward and mark it completed. Reuses the enemy reward pipeline so
        /// exp/gold/item drops apply (and level-ups happen) exactly as they do for kills.
        /// </summary>
        public static bool Claim(string questId)
        {
            PlayerProfileService.EnsureLoaded();
            var profile = PlayerProfileService.Current;

            var progress = FindEntry(profile, questId);
            if (progress == null) return false;
            if (!QuestCatalog.TryGet(questId, out var def)) return false;

            // Make sure progress reflects the latest stats before allowing a claim.
            EvaluateOne(progress);
            if (progress.State != QuestState.ReadyToClaim) return false;

            RewardGranter.Grant(def.Reward, foldIntoRun: false);

            progress.State = QuestState.Completed;
            progress.CompletedAtTicks = DateTime.UtcNow.Ticks;

            PlayerProfileService.Save();
            return true;
        }

        /// <summary>Recompute every active quest's state; promotes finished ones to ReadyToClaim.</summary>
        public static void Evaluate()
        {
            PlayerProfileService.EnsureLoaded();
            Evaluate(PlayerProfileService.Current);
        }

        // --- Internals ---

        private static void Evaluate(PlayerProfile profile)
        {
            if (profile?.QuestLog == null) return;
            foreach (var progress in profile.QuestLog)
                EvaluateOne(progress);
        }

        private static void EvaluateOne(QuestProgress progress)
        {
            if (progress == null || progress.State == QuestState.Completed) return;

            bool allMet = true;
            if (progress.Objectives != null)
            {
                foreach (var objective in progress.Objectives)
                {
                    if (CurrentAmount(objective) < objective.Required)
                    {
                        allMet = false;
                        break;
                    }
                }
            }

            progress.State = allMet ? QuestState.ReadyToClaim : QuestState.Active;
        }

        private static bool IsAvailable(PlayerProfile profile, QuestDefinition def)
        {
            var entry = FindEntry(profile, def.Id);
            if (entry == null) return true;

            // An accepted (Active/ReadyToClaim) quest is never re-offered.
            if (entry.State != QuestState.Completed) return false;

            // Completed: one-shot quests stay gone; daily/weekly return once the period rolls over.
            switch (def.CategoryValue)
            {
                case QuestCategory.Daily:  return !IsSamePeriod(entry.CompletedAtTicks, Period.Day);
                case QuestCategory.Weekly: return !IsSamePeriod(entry.CompletedAtTicks, Period.Week);
                default:                   return false;
            }
        }

        private static QuestProgress FindEntry(PlayerProfile profile, string questId)
        {
            if (profile?.QuestLog == null || string.IsNullOrEmpty(questId)) return null;
            return profile.QuestLog.Find(p => p != null && p.QuestId == questId);
        }

        private enum Period { Day, Week }

        /// <summary>True when <paramref name="ticks"/> falls in the same local day/week as now.</summary>
        private static bool IsSamePeriod(long ticks, Period period)
        {
            if (ticks == 0) return false;
            DateTime then = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
            DateTime now = DateTime.Now;

            if (period == Period.Day)
                return then.Date == now.Date;

            // Week: same ISO-style week (Monday start) and year.
            return then.Year == now.Year && WeekOfYear(then) == WeekOfYear(now);
        }

        private static int WeekOfYear(DateTime date)
        {
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(date,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        private static int Clamp(int value, int min, int max) =>
            value < min ? min : (value > max ? max : value);
    }
}
