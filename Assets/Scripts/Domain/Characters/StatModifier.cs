namespace DwarfsCrypt.Domain.Characters
{
    public struct StatModifier
    {
        public readonly AttributeType Attribute;
        public readonly ModifierType Type;
        public readonly float Value;
        public readonly string Source;

        public StatModifier(AttributeType attribute, ModifierType type, float value, string source)
        {
            Attribute = attribute;
            Type = type;
            Value = value;
            Source = source;
        }
    }
}
