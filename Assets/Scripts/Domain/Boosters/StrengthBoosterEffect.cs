using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Domain.Boosters
{
    public class StrengthBoosterEffect : BoosterEffect
    {
        private readonly float _bonus;
        private readonly float _duration;

        public StrengthBoosterEffect(float bonus = 10f, float duration = 120f)
        {
            _bonus = bonus;
            _duration = duration;
        }

        public override string DisplayName => "Strength Boost";
        public override float Duration => _duration;
        public override string Source => "StrengthBooster";

        public override void Apply(CharacterAttributes attributes)
        {
            // Boost Strength so any stat readout reflects the buff...
            attributes.AddModifier(new StatModifier(
                AttributeType.Strength,
                ModifierType.Flat,
                _bonus,
                Source
            ));

            // ...and mirror it into PhysicalAttack, the derived stat combat actually reads.
            // (Derived stats are baked from the base stats at construction and don't recompute
            // from runtime Strength modifiers, so the buff must feed PhysicalAttack directly.)
            attributes.AddModifier(new StatModifier(
                AttributeType.PhysicalAttack,
                ModifierType.Flat,
                _bonus * AttributeFormulas.PhysicalAttackPerStrength,
                Source
            ));
        }
    }
}
