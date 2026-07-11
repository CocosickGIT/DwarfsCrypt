using System;
using DwarfsCrypt.Domain.Afk;
using DwarfsCrypt.Domain.Characters;
using UnityEngine;

namespace Core.Afk
{
    /// <summary>
    /// Pure, stat-driven combat simulation for an AFK Farming run. The player fights a queue of
    /// identical mobs one-on-one; HP carries over between mobs (no healing). Both sides swing on
    /// their own cadence derived from <see cref="AttributeType.AttackSpeed"/>, and each blow rolls
    /// real physical damage via <see cref="AttributeFormulas.RollPhysicalDamage"/> — the same roll
    /// the live game uses — so the result reflects the player's actual stats and gear.
    ///
    /// Damage application mirrors <c>Character.TakeDamage</c> (rounded, minimum 1 per hit), which
    /// also guarantees termination: every player swing removes at least 1 HP from the current mob,
    /// and the simulation clock only advances, so it cannot loop forever.
    /// </summary>
    public static class AfkRunSimulator
    {
        /// <summary>Seconds between attacks at AttackSpeed = 1. Higher AttackSpeed shortens this.</summary>
        public const float BaseAttackInterval = 1.2f;

        public static AfkRunResult Simulate(
            CharacterAttributes player, int playerMaxHp,
            CharacterAttributes enemy, int enemyMaxHp,
            int mobCount, float duration)
        {
            var result = new AfkRunResult { MobCount = mobCount };
            if (player == null || enemy == null || mobCount <= 0 || duration <= 0f)
            {
                result.PlayerSurvived = true;
                return result;
            }

            float playerStep = AttackInterval(player);
            float enemyStep   = AttackInterval(enemy);

            float playerHp = playerMaxHp;
            float enemyHp  = enemyMaxHp;

            float time      = 0f;
            float playerNext = playerStep;   // time of the player's next swing
            float enemyNext  = enemyStep;    // time of the current mob's next swing

            while (result.Kills < mobCount)
            {
                // Advance to whichever combatant swings next; stop if it falls outside the run window.
                float next = Mathf.Min(playerNext, enemyNext);
                if (next > duration) break;
                time = next;

                // Ties go to the player so a finishing blow lands before the mob can retaliate.
                if (playerNext <= enemyNext)
                {
                    enemyHp -= RollDamage(player);
                    playerNext += playerStep;

                    if (enemyHp <= 0f)
                    {
                        result.Kills++;
                        if (result.Kills >= mobCount) break;

                        // Next mob steps in at full HP and starts its own swing timer from now.
                        enemyHp = enemyMaxHp;
                        enemyNext = time + enemyStep;
                    }
                }
                else
                {
                    playerHp -= RollDamage(enemy);
                    enemyNext += enemyStep;

                    if (playerHp <= 0f)
                    {
                        result.PlayerSurvived = false;
                        result.DurationUsed = time;
                        return result;
                    }
                }
            }

            result.PlayerSurvived = true;
            result.DurationUsed = time;
            return result;
        }

        private static float AttackInterval(CharacterAttributes attrs)
        {
            float speed = attrs.GetFinal(AttributeType.AttackSpeed);
            if (speed <= 0.01f) speed = 1f;
            return BaseAttackInterval / speed;
        }

        // Mirrors Character.TakeDamage: round away from zero, floor at 1 damage per hit.
        private static float RollDamage(CharacterAttributes attrs)
        {
            DamageResult dmg = AttributeFormulas.RollPhysicalDamage(attrs);
            return Math.Max(1f, (float)Math.Round(dmg.Amount, MidpointRounding.AwayFromZero));
        }
    }
}
