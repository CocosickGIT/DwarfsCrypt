using System;

namespace DwarfsCrypt.Domain.Skills
{
    // Data-driven definition of one skill, loaded from Resources/Skills/skills.json via JsonUtility.
    // Field names must match the JSON keys exactly (PascalCase, like the other configs).
    [Serializable]
    public class SkillConfig
    {
        public string Id;             // unique id, referenced by the SkillCaster slots
        public string Name;           // display name
        public string Effect;         // SkillEffectType name, parsed by EffectType below
        public string Icon;           // optional Resources path to a UI icon, no extension

        public float Cooldown;        // seconds before the skill can be used again
        public float ManaCost;        // mana spent per cast; 0 = free
        public float DamageMultiplier = 1f; // scales a rolled PhysicalAttack hit

        // MeleeArc parameters
        public float Radius;          // hit radius around the caster
        public float HalfAngle;       // arc half-angle in degrees (180 = full circle)

        public float Duration;        // reserved for timed-effect skills (buffs, etc.)

        public SkillEffectType EffectType =>
            Enum.TryParse(Effect, ignoreCase: true, out SkillEffectType type)
                ? type
                : SkillEffectType.MeleeArc;
    }
}
