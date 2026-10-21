using System;

namespace DwarfsCrypt.Domain.Towns
{
    /// <summary>
    /// A town's position on the world map, in normalized map coordinates: (0,0) is the bottom-left
    /// corner of the map image, (1,1) the top-right. Normalized so the same authored values work at
    /// any map resolution or aspect the UI is laid out at.
    /// </summary>
    [Serializable]
    public class MapPoint
    {
        public float X;
        public float Y;
    }
}
