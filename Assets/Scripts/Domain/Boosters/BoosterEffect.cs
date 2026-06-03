using DwarfsCrypt.Domain.Characters;

namespace DwarfsCrypt.Domain.Boosters
{
    public abstract class BoosterEffect
    {
        public abstract string DisplayName { get; }
        public abstract float Duration { get; }
        public abstract string Source { get; }

        public abstract void Apply(CharacterAttributes attributes);

        public virtual void Remove(CharacterAttributes attributes)
        {
            attributes.RemoveModifiersFromSource(Source);
        }
    }
}
