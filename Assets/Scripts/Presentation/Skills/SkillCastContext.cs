using UnityEngine;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Skills;
using DwarfsCrypt.Presentation.Combat;

namespace DwarfsCrypt.Presentation.Skills
{
    // Everything a skill effect needs at cast time. Built by the SkillCaster and handed to
    // the resolved ISkillEffect. Read-only struct so effects can't mutate the wiring.
    public readonly struct SkillCastContext
    {
        public readonly SkillConfig Config;
        public readonly Character Caster;
        public readonly MeleeAttackHitbox Hitbox;
        public readonly Vector2 AimDirection;
        public readonly float AttackDuration;

        public SkillCastContext(
            SkillConfig config,
            Character caster,
            MeleeAttackHitbox hitbox,
            Vector2 aimDirection,
            float attackDuration)
        {
            Config = config;
            Caster = caster;
            Hitbox = hitbox;
            AimDirection = aimDirection;
            AttackDuration = attackDuration;
        }
    }
}
