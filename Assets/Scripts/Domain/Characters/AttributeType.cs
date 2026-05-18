namespace DwarfsCrypt.Domain.Characters
{
    public enum AttributeType
    {
        // Primary
        Strength,
        Dexterity,
        Constitution,
        Wit,
        Mentality,
        Luck,

        // Resources
        MaxHp,
        MaxStamina,
        MaxMana,

        // Offense
        PhysicalAttack,
        MagicAttack,
        AttackSpeed,
        CritChance,
        CritDamage,

        // Defense
        PhysicalDefense,
        MagicDefense,

        // Movement
        MoveSpeed,
    }
}
