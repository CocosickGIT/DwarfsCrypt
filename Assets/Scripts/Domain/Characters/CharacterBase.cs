namespace DwarfsCrypt.Domain.Characters
{
    public abstract class CharacterBase
    {
        public string Name { get; protected set; }
        public Race Race { get; protected set; }
        public CharacterStats Stats { get; protected set; }

        protected CharacterBase(string name, Race race, CharacterStats stats)
        {
            Name = name;
            Race = race;
            Stats = stats;
        }
    }
}
