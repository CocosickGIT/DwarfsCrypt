namespace DwarfsCrypt.Presentation.Skills
{
    // Behavior strategy for a skill. Concrete effects (e.g. MeleeArcSkillEffect) own HOW a skill
    // acts; the SkillConfig owns the numbers. Mirrors the BoosterEffect strategy split.
    public interface ISkillEffect
    {
        void Execute(in SkillCastContext ctx);
    }
}
