using System;

namespace DwarfsCrypt.Domain.Statistics
{
    /// <summary>
    /// A single persisted lifetime counter (e.g. "kill:Skeleton" -> 217). Stored as a flat list on
    /// the player profile because JsonUtility cannot serialize dictionaries. See <see cref="StatKeys"/>
    /// for the key format.
    /// </summary>
    [Serializable]
    public class StatEntry
    {
        public string Key;
        public int Value;

        public StatEntry() { }

        public StatEntry(string key, int value)
        {
            Key = key;
            Value = value;
        }
    }
}
