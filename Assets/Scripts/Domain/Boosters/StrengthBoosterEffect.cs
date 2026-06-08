using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Domain.Boosters
{
    public class StrengthBoosterEffect : BoosterEffect
    {
        private readonly float _bonus;
        private readonly float _duration;

        public StrengthBoosterEffect(float bonus = 10f, float duration = 5f)
        {
            _bonus = bonus;
            _duration = duration;
        }

        public override string DisplayName => "Strength Boost";
        public override float Duration => _duration;
        public override string Source => "StrengthBooster";

        public override void Apply(CharacterAttributes attributes)
        {
            attributes.AddModifier(new StatModifier(
                AttributeType.Strength,
                ModifierType.Flat,
                _bonus,
                Source
            ));
        }
    }
}
