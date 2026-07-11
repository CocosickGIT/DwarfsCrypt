namespace DwarfsCrypt.Domain.Skills
{
    // Selects which behavior strategy a skill runs. JSON owns the numbers; this enum
    // selects the code that interprets them (see SkillCaster's effect factory).
    public enum SkillEffectType
    {
        MeleeArc, // melee hit over a radius + half-angle around the caster (wide swing or full-circle smash)
    }
}
