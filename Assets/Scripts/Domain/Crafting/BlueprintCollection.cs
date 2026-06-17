using System;
using System.Collections.Generic;

namespace DwarfsCrypt.Domain.Crafting
{
    /// <summary>
    /// Serialization root for StreamingAssets/Crafting/blueprints.json, mirroring
    /// <c>ItemsCollection</c> for items.
    /// </summary>
    [Serializable]
    public class BlueprintCollection
    {
        public List<Blueprint> Blueprints;
    }
}
