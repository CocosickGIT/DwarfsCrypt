using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Towns
{
    [Serializable]
    public class TownConfig
    {
        public string Id;
        public string Name;
        public LevelBand LevelBand;
        public List<TownModifier> Modifiers;
    }
}
