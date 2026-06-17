using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DwarfsCrypt.Environment
{
    /// <summary>
    /// Invisible collision walls around a rectangular play area, keeping the player
    /// and enemies inside the map. Right-click the component header and choose
    /// "Build Walls" / "Clear Walls". The area is centered on this transform's
    /// position and matches the XY rectangle used by <see cref="EnvironmentScatter"/>.
    /// </summary>
    public class MapBoundary : MonoBehaviour
    {
        [Header("Area (XY rectangle, centered on this transform)")]
        public Vector2 areaSize = new Vector2(50f, 50f);

        [Header("Walls")]
        [Tooltip("How thick each wall is. Keep it generous so fast movers can't tunnel through.")]
        public float wallThickness = 2f;

        [Tooltip("Physics layer the generated walls are placed on. Collide this layer with the " +
                 "Player and Enemy layers so neither can leave the map. Keep it OUT of the " +
                 "player's Dash Phase Layers so a dashing player still hits the walls.")]
        public string wallLayer = "Boundary";

        private const string WallsRootName = "_BoundaryWalls";

        [ContextMenu("Build Walls")]
        public void BuildWalls()
        {
            ClearWalls();

            var root = new GameObject(WallsRootName);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(root, "Build Boundary Walls");
#endif
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;

            float halfX = areaSize.x * 0.5f;
            float halfY = areaSize.y * 0.5f;
            float t = Mathf.Max(0.01f, wallThickness);
            float halfT = t * 0.5f;

            // Each wall sits just OUTSIDE the area so its inner face aligns with the edge.
            // Horizontal walls overlap the corners (extended by thickness) to seal gaps.
            CreateWall(root.transform, "Wall_Top",    new Vector2(0f,  halfY + halfT), new Vector2(areaSize.x + t * 2f, t));
            CreateWall(root.transform, "Wall_Bottom", new Vector2(0f, -halfY - halfT), new Vector2(areaSize.x + t * 2f, t));
            CreateWall(root.transform, "Wall_Right",  new Vector2( halfX + halfT, 0f), new Vector2(t, areaSize.y));
            CreateWall(root.transform, "Wall_Left",   new Vector2(-halfX - halfT, 0f), new Vector2(t, areaSize.y));
        }

        private void CreateWall(Transform parent, string name, Vector2 localCenter, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.layer = ResolveLayer();

            var box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        private int ResolveLayer()
        {
            int layer = LayerMask.NameToLayer(wallLayer);
            if (layer < 0)
            {
                Debug.LogWarning($"MapBoundary: layer '{wallLayer}' does not exist. Falling back to 'Default'.", this);
                layer = 0;
            }
            return layer;
        }

        [ContextMenu("Clear Walls")]
        public void ClearWalls()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != WallsRootName) continue;
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(child.gameObject);
#else
                DestroyImmediate(child.gameObject);
#endif
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, areaSize.y, 0.1f));
        }
    }
}
