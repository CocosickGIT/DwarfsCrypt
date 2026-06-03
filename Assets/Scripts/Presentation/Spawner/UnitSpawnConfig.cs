using System;
using System.Collections.Generic;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Spawner
{

    [Serializable]
    public class UnitSpawnConfig
    {
        public GameObject Prefab;
        [Tooltip("StreamingAssets path without extension, e.g. Characters/skeleton")]
        public string ConfigPath;
        public List<Transform> SpawnPoints = new();
    }
}
