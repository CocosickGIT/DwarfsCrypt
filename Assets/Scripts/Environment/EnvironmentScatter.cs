using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DwarfsCrypt.Environment
{
    /// <summary>
    /// Editor-baked procedural scatter for a flat ground plane.
    /// Algorithm: Perlin-noise density map -> Poisson-disc placement -> layered passes (big to small).
    /// Right-click the component header and choose "Generate" / "Clear".
    /// </summary>
    public class EnvironmentScatter : MonoBehaviour
    {
        [System.Serializable]
        public class ScatterLayer
        {
            public string name = "Trees";
            public GameObject[] prefabs;        // one picked at random per placement

            [Header("Spacing")]
            [Tooltip("Minimum distance between objects of THIS layer.")]
            public float minRadius = 4f;
            [Tooltip("Extra clearance this object demands from objects placed in EARLIER layers.")]
            public float avoidPreviousRadius = 2f;

            [Header("Density (Perlin)")]
            [Range(0f, 1f)] public float coverage = 0.6f;   // 0 = none, 1 = pack the whole area
            public float noiseScale = 0.05f;                 // smaller = larger groves/clearings
            [Range(0f, 1f)] public float noiseThreshold = 0.4f; // below this noise value -> bald patch

            [Header("Variation")]
            public Vector2 scaleRange = new Vector2(0.85f, 1.2f);
            public bool randomYRotation = true;
        }

        [Header("Area (flat plane, local XZ)")]
        public Vector2 areaSize = new Vector2(50f, 50f);
        public float groundY = 0f;

        [Header("Determinism")]
        public int seed = 12345;

        [Header("Layers (placed top to bottom: big -> small)")]
        public ScatterLayer[] layers;

        // Placed points across ALL layers, used for cross-layer avoidance.
        // Each entry: local-space xz position + the exclusion radius it carries.
        private readonly List<Vector3> _placed = new List<Vector3>();
        private readonly List<float> _placedRadius = new List<float>();

        [ContextMenu("Generate")]
        public void Generate()
        {
            Clear();
            var rng = new System.Random(seed);

            foreach (var layer in layers)
            {
                if (layer.prefabs == null || layer.prefabs.Length == 0) continue;
                PlaceLayer(layer, rng);
            }
        }

        private void PlaceLayer(ScatterLayer layer, System.Random rng)
        {
            // Poisson-disc (Bridson) candidates for this layer's own spacing.
            foreach (var p in PoissonDisc(layer.minRadius, rng))
            {
                // Perlin density gate -> creates groves & clearings.
                float n = Mathf.PerlinNoise(
                    (p.x + seed) * layer.noiseScale,
                    (p.y + seed) * layer.noiseScale);
                if (n < layer.noiseThreshold) continue;
                if (rng.NextDouble() > layer.coverage) continue;

                var local = new Vector3(p.x - areaSize.x * 0.5f, p.y - areaSize.y * 0.5f, groundY);

                // Cross-layer avoidance (e.g. rocks keeping clear of trees).
                if (TooCloseToPrevious(local, layer.avoidPreviousRadius)) continue;

                Spawn(layer, local, rng);
                _placed.Add(local);
                _placedRadius.Add(layer.avoidPreviousRadius);
            }
        }

        private bool TooCloseToPrevious(Vector3 local, float ownClearance)
        {
            for (int i = 0; i < _placed.Count; i++)
            {
                float min = Mathf.Max(ownClearance, _placedRadius[i]);
                if ((_placed[i] - local).sqrMagnitude < min * min) return true;
            }
            return false;
        }

        private void Spawn(ScatterLayer layer, Vector3 local, System.Random rng)
        {
            var prefab = layer.prefabs[rng.Next(layer.prefabs.Length)];
            GameObject go;
#if UNITY_EDITOR
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
            Undo.RegisterCreatedObjectUndo(go, "Scatter");
#else
            go = Instantiate(prefab, transform);
#endif
            go.transform.localPosition = local;

            if (layer.randomYRotation)
                go.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            float s = Mathf.Lerp(layer.scaleRange.x, layer.scaleRange.y, (float)rng.NextDouble());
            go.transform.localScale *= s;
        }

        /// <summary>Bridson Poisson-disc sampling over [0,areaSize]. Returns local XZ points.</summary>
        private List<Vector2> PoissonDisc(float radius, System.Random rng)
        {
            const int k = 30; // candidates per active point
            float cell = radius / Mathf.Sqrt(2f);
            int gx = Mathf.CeilToInt(areaSize.x / cell);
            int gy = Mathf.CeilToInt(areaSize.y / cell);
            var grid = new int[gx, gy];
            for (int x = 0; x < gx; x++)
                for (int y = 0; y < gy; y++) grid[x, y] = -1;

            var points = new List<Vector2>();
            var active = new List<Vector2>();

            var first = new Vector2((float)rng.NextDouble() * areaSize.x, (float)rng.NextDouble() * areaSize.y);
            AddPoint(first, points, active, grid, cell);

            while (active.Count > 0)
            {
                int idx = rng.Next(active.Count);
                var center = active[idx];
                bool found = false;

                for (int i = 0; i < k; i++)
                {
                    float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                    float dist = radius * (1f + (float)rng.NextDouble()); // ring [r, 2r)
                    var cand = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;

                    if (cand.x < 0 || cand.x >= areaSize.x || cand.y < 0 || cand.y >= areaSize.y) continue;
                    if (!IsFarEnough(cand, points, grid, cell, gx, gy, radius)) continue;

                    AddPoint(cand, points, active, grid, cell);
                    found = true;
                    break;
                }

                if (!found) active.RemoveAt(idx);
            }
            return points;
        }

        private static void AddPoint(Vector2 p, List<Vector2> points, List<Vector2> active, int[,] grid, float cell)
        {
            grid[(int)(p.x / cell), (int)(p.y / cell)] = points.Count;
            points.Add(p);
            active.Add(p);
        }

        private static bool IsFarEnough(Vector2 cand, List<Vector2> points, int[,] grid, float cell, int gx, int gy, float radius)
        {
            int cx = (int)(cand.x / cell);
            int cy = (int)(cand.y / cell);
            float r2 = radius * radius;
            for (int x = Mathf.Max(0, cx - 2); x <= Mathf.Min(gx - 1, cx + 2); x++)
            {
                for (int y = Mathf.Max(0, cy - 2); y <= Mathf.Min(gy - 1, cy + 2); y++)
                {
                    int pi = grid[x, y];
                    if (pi >= 0 && (points[pi] - cand).sqrMagnitude < r2) return false;
                }
            }
            return true;
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            _placed.Clear();
            _placedRadius.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
#else
                DestroyImmediate(transform.GetChild(i).gameObject);
#endif
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0, groundY, 0), new Vector3(areaSize.x, areaSize.y, 0.1f));
        }
    }
}
