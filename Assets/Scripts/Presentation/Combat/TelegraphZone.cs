using System.Collections;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    // Ground telegraph for boss attacks (Don't Starve style): the full danger zone outline
    // appears the moment the boss commits, then an inner fill grows toward the edge over the
    // wind-up. Full fill = the hit lands. Visual only — the caller applies the damage when
    // the wind-up ends.
    //
    // Lives detached in world space (never parented under the boss) so it is unaffected by
    // the boss's sprite-flip localScale and stays put if the boss is nudged.
    public class TelegraphZone : MonoBehaviour
    {
        private const int ArcSegments = 48;
        private const string SortingLayerName = "Default";
        private const int SortingOrder = 150; // under AttackSwipeVFX (200), over the ground

        // Brief pause with the zone fully filled at detonation, so "full = boom" is readable.
        private const float DetonationHoldTime = 0.15f;

        private static readonly Color BaseColor = new Color(1f, 0.25f, 0.15f, 0.20f);
        private static readonly Color FillColor = new Color(1f, 0.40f, 0.15f, 0.45f);

        private MeshFilter _baseFilter;
        private MeshFilter _fillFilter;
        private MeshRenderer _baseRenderer;
        private MeshRenderer _fillRenderer;
        private Transform _fillTransform;
        private Coroutine _routine;

        public static TelegraphZone Create(string zoneName)
        {
            var go = new GameObject(zoneName);
            return go.AddComponent<TelegraphZone>();
        }

        private void Awake()
        {
            (_baseFilter, _baseRenderer) = CreateLayer("Base", BaseColor, SortingOrder);
            (_fillFilter, _fillRenderer) = CreateLayer("Fill", FillColor, SortingOrder + 1);
            _fillTransform = _fillFilter.transform;
        }

        private (MeshFilter, MeshRenderer) CreateLayer(string childName, Color color, int order)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            var filter = child.AddComponent<MeshFilter>();
            filter.mesh = new Mesh { name = $"Telegraph{childName}Mesh" };

            var meshRenderer = child.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = color };
            meshRenderer.sortingLayerName = SortingLayerName;
            meshRenderer.sortingOrder = order;
            meshRenderer.enabled = false;

            return (filter, meshRenderer);
        }

        // Locks the zone at origin/direction and fills it over duration seconds.
        public void Show(Vector2 origin, Vector2 direction, float radius, float halfAngle, float duration)
        {
            transform.position = origin;

            BuildFan(_baseFilter.mesh, direction, radius, halfAngle);
            BuildFan(_fillFilter.mesh, direction, radius, halfAngle);

            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(FillRoutine(duration));
        }

        public void Hide()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            _baseRenderer.enabled = false;
            _fillRenderer.enabled = false;
        }

        private IEnumerator FillRoutine(float duration)
        {
            _fillTransform.localScale = Vector3.zero;
            _baseRenderer.enabled = true;
            _fillRenderer.enabled = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                _fillTransform.localScale = new Vector3(t, t, 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _fillTransform.localScale = Vector3.one;
            yield return new WaitForSeconds(DetonationHoldTime);

            _baseRenderer.enabled = false;
            _fillRenderer.enabled = false;
            _routine = null;
        }

        // Fan mesh from local origin — uniform scaling of the fill child scales its radius,
        // which is what animates the fill. halfAngle 180 produces a full circle.
        private static void BuildFan(Mesh mesh, Vector2 direction, float radius, float halfAngle)
        {
            int vertCount = ArcSegments + 2;
            var verts = new Vector3[vertCount];
            var tris = new int[ArcSegments * 3];

            verts[0] = Vector3.zero;

            for (int i = 0; i <= ArcSegments; i++)
            {
                float t = i / (float)ArcSegments;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                Vector2 w = Rotate(direction, angle) * radius;
                verts[i + 1] = new Vector3(w.x, w.y, 0f);
            }

            for (int i = 0; i < ArcSegments; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            mesh.Clear();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
