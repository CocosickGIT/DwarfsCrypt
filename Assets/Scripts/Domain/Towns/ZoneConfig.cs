using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Towns
{
    [Serializable]
    public class ZoneConfig
    {
        public string Id;
        public string TownId;
        public int MinLevel;
        public List<MonsterEntry> MonsterTable;
    }
}
