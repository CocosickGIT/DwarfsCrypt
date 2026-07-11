using UnityEngine;
using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Presentation.Skills
{
    // Rolls a physical hit (same math as a basic attack), scales it by the skill's
    // DamageMultiplier, and applies it over the skill's radius + half-angle. Drives both the
    // Wide Attack (wide forward arc) and Ground Smash (full 360° circle, HalfAngle = 180).
    public sealed class MeleeArcSkillEffect : ISkillEffect
    {
        public void Execute(in SkillCastContext ctx)
        {
            DamageResult damage =
                AttributeFormulas.RollPhysicalDamage(ctx.Caster.Attributes)
                * Mathf.Max(0f, ctx.Config.DamageMultiplier);

            ctx.Hitbox.PerformAttack(
                ctx.AimDirection,
                damage,
                ctx.AttackDuration,
                ctx.Config.Radius,
                ctx.Config.HalfAngle);
        }
    }
}
