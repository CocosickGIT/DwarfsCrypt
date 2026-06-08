using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DwarfsCrypt.Presentation.Spawner
{
    [Serializable]
    public class CampEnemyEntry
    {
        public GameObject Prefab;
        [Tooltip("StreamingAssets path without extension, e.g. Characters/skeleton")]
        public string ConfigPath;
        [Min(0.01f), Tooltip("Relative spawn weight. Higher = more common. Player Luck compresses differences, making rarer (lower-weight) units proportionally more likely.")]
        public float Weight = 1f;
    }

    public class CampSpawnZone : MonoBehaviour
    {
        [Min(0f)]
        public float Radius = 5f;

        [Header("Spawn Count")]
        [Min(1)]
        public int Count = 1;
        [Tooltip("Spawn additional enemies on top of base Count")]
        public bool AddExtra;
        [Min(0)]
        public int ExtraCount; //possibly not needed

        public int TotalCount => Count + (AddExtra ? ExtraCount : 0);

        [Header("Enemy Types")]
        public List<CampEnemyEntry> Enemies = new();

        [SerializeField, Min(2), Tooltip("Candidates tested per point — higher = more even spread, tiny perf cost")]
        private int _candidatesPerPoint = 12;

        // luck = player's final Luck attribute value (0 = no bonus, ~100 = strong rare bias).
        // Each slot rolls a weighted pick; luck power-compresses the weights so rarer entries
        // (lower Weight) become proportionally cheaper as luck rises.
        public IEnumerable<(GameObject prefab, string configPath, Vector3 position)> GetSpawnData(float luck = 0f)
        {
            var valid = new List<CampEnemyEntry>();
            foreach (var entry in Enemies)
            {
                if (entry.Prefab == null || entry.Weight <= 0f) continue;
                valid.Add(entry);
            }

            int totalSlots = TotalCount;
            if (totalSlots == 0 || valid.Count == 0) yield break;

            // luckFactor in [1, ∞): compresses the weight range — as it rises, all entries
            // trend toward equal probability, so rarer units get a relative boost.
            float luckFactor = 1f + Mathf.Min(luck, 100f) * 0.01f;

            var effectiveWeights = new float[valid.Count];
            float totalWeight = 0f;
            for (int i = 0; i < valid.Count; i++)
            {
                effectiveWeights[i] = Mathf.Pow(valid[i].Weight, 1f / luckFactor);
                totalWeight += effectiveWeights[i];
            }

            // luck/100 = flat probability for one bonus enemy beyond the designed count.
            // At luck=0: 0% chance. At luck=100: guaranteed +1.
            int bonusSlots = Random.value < Mathf.Clamp01(luck * 0.1f) ? Random.Range(1,2) : 0;
            int totalPositions = totalSlots + bonusSlots;

            var positions = GenerateEvenPositions(totalPositions);

            for (int slot = 0; slot < totalPositions; slot++)
            {
                int idx = RollWeighted(effectiveWeights, totalWeight);
                var entry = valid[idx];
                yield return (entry.Prefab, entry.ConfigPath, positions[slot]);
            }
        }

        private static int RollWeighted(float[] weights, float total)
        {
            float roll = Random.Range(0f, total);
            for (int i = 0; i < weights.Length; i++)
            {
                if (roll < weights[i]) return i;
                roll -= weights[i];
            }
            return weights.Length - 1;
        }

        // Best-candidate sampling: for each new point, pick the candidate that is
        // farthest from all already-placed points. Produces even coverage with no
        // special data structures — cost is O(count * candidates * count).
        private List<Vector3> GenerateEvenPositions(int count)
        {
            var placed = new List<Vector3>(count);

            for (int i = 0; i < count; i++)
            {
                Vector3 best = RandomPointInZone();
                float bestDist = MinSqDistToPlaced(best, placed);

                for (int c = 1; c < _candidatesPerPoint; c++)
                {
                    Vector3 candidate = RandomPointInZone();
                    float d = MinSqDistToPlaced(candidate, placed);
                    if (d > bestDist)
                    {
                        bestDist = d;
                        best = candidate;
                    }
                }

                placed.Add(best);
            }

            return placed;
        }

        private static float MinSqDistToPlaced(Vector3 point, List<Vector3> placed)
        {
            if (placed.Count == 0) return float.MaxValue;

            float min = float.MaxValue;
            foreach (var p in placed)
            {
                float d = (point - p).sqrMagnitude;
                if (d < min) min = d;
            }
            return min;
        }

        private Vector3 RandomPointInZone()
        {
            Vector2 circle = Random.insideUnitCircle * Radius;
            return transform.position + new Vector3(circle.x, circle.y, 0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.6f);
            DrawCircle(transform.position, Radius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.15f);
            // Filled disc approximated with thin rings
            int rings = 8;
            for (int r = 1; r <= rings; r++)
                DrawCircle(transform.position, Radius * r / rings);
        }

        private static void DrawCircle(Vector3 center, float radius)
        {
            const int segments = 36;
            float step = 2f * Mathf.PI / segments;
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
