namespace Core.Player
{
    /// <summary>
    /// Lightweight summary of a save slot for the save-select UI, read without making the slot
    /// active. <see cref="Exists"/> is false for an empty slot (a "New Game" entry).
    /// </summary>
    public struct SaveSlotInfo
    {
        public int Slot;
        public bool Exists;
        public string Name;
        public string Race;
        public int Level;
        public int Gold;

        public static SaveSlotInfo Empty(int slot) => new SaveSlotInfo { Slot = slot, Exists = false };
    }
}
