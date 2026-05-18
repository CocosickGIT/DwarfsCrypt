using System;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Spawner
{
    [Serializable]
    public class EnemySpawnEntry
    {
        public GameObject Prefab;
        public Transform SpawnPoint;
    }
}
