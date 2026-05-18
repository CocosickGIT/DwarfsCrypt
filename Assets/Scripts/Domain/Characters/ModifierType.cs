namespace DwarfsCrypt.Domain.Characters
{
    public enum ModifierType
    {
        Flat,         // added directly: base + value
        PercentBase,  // scales the base before flat: base * (1 + sum)
        PercentFinal, // scales after all flat bonuses: (base + flat) * (1 + sum)
    }
}
