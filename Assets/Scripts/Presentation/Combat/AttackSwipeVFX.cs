using System.Collections;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AttackSwipeVFX : MonoBehaviour
    {
        [SerializeField] private Color _color = new Color(1f, 0.85f, 0.35f, 0.8f);
        [SerializeField] private int _arcSegments = 20;
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _sortingOrder = 200;

        [Tooltip("Alpha over normalized attack time: x=time, y=alpha multiplier")]
        [SerializeField] private AnimationCurve _alphaCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 8f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _material;
        private Coroutine _activeRoutine;

        private void Awake()
        {
            _meshFilter  = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter.mesh = new Mesh { name = "AttackSwipeMesh" };

            _material = new Material(Shader.Find("Sprites/Default"));
            _meshRenderer.material = _material;
            _meshRenderer.sortingLayerName = _sortingLayerName;
            _meshRenderer.sortingOrder = _sortingOrder;
            _meshRenderer.enabled = false;
        }

        public void Play(Vector2 direction, float radius, float halfAngle, float duration)
        {
            if (_activeRoutine != null)
                StopCoroutine(_activeRoutine);

            BuildMesh(direction, radius, halfAngle);
            _activeRoutine = StartCoroutine(FadeRoutine(duration));
        }

        private void BuildMesh(Vector2 direction, float radius, float halfAngle)
        {
            // Compensate for sprite-flip scale so the cone points in world-space direction.
            // PlayerController negates transform.localScale.x when facing right.
            float invX = Mathf.Approximately(transform.lossyScale.x, 0f) ? 1f : 1f / transform.lossyScale.x;
            float invY = Mathf.Approximately(transform.lossyScale.y, 0f) ? 1f : 1f / transform.lossyScale.y;

            int vertCount = _arcSegments + 2;
            var verts = new Vector3[vertCount];
            var tris  = new int[_arcSegments * 3];

            verts[0] = Vector3.zero;

            for (int i = 0; i <= _arcSegments; i++)
            {
                float t     = i / (float)_arcSegments;
                float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
                Vector2 w   = Rotate(direction, angle) * radius;
                verts[i + 1] = new Vector3(w.x * invX, w.y * invY, 0f);
            }

            for (int i = 0; i < _arcSegments; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            Mesh mesh = _meshFilter.mesh;
            mesh.Clear();
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        private IEnumerator FadeRoutine(float duration)
        {
            _meshRenderer.enabled = true;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float alpha = _alphaCurve.Evaluate(elapsed / duration) * _color.a;
                _material.color = new Color(_color.r, _color.g, _color.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _meshRenderer.enabled = false;
            _activeRoutine = null;
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
