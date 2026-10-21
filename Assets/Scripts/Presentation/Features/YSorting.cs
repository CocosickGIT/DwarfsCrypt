using UnityEngine;
using UnityEngine.Rendering;

namespace Presentation.Features
{
    /// <summary>
    /// Sorts a sprite (or sprite group) against every other sprite in the scene
    /// by the world-Y of a single "sort point" on the model. Whichever object's
    /// sort point is lower on screen is drawn in front. Put the sort point at the
    /// base of the model (character's feet, box's bottom edge) so things overlap
    /// correctly as they cross each other.
    /// </summary>
    [DisallowMultipleComponent]
    public class YSorting : MonoBehaviour
    {
        [Header("Renderer (auto-detected if left empty)")]
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private SortingGroup sg;

        [Header("Sort point")]
        [Tooltip("The point on the model used to compare against other objects. " +
                 "Place it at the base (feet / bottom edge). Falls back to this transform if empty.")]
        [SerializeField] private Transform sortPoint;

        [Header("Tuning")]
        [Tooltip("Higher = finer sorting resolution. 1 world unit becomes this many sorting-order steps.")]
        [SerializeField] private float multiplier = 100f;
        [SerializeField] private int offset = 0;

        [Tooltip("If the object never moves, sort once on Awake instead of every frame.")]
        [SerializeField] private bool isStatic = false;

        // Last order actually written, so we only touch the renderer when it changes. Writing
        // sortingOrder dirties the renderer, so skipping unchanged writes matters for objects that
        // are technically dynamic but momentarily still.
        private int _lastOrder = int.MinValue;

        private void Awake()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sg == null) sg = GetComponent<SortingGroup>();
            if (sortPoint == null) sortPoint = transform;
        }

        private void Start()
        {
            if (isStatic)
            {
                // Sort once, then disable so Unity stops invoking LateUpdate on this component
                // entirely — a scene full of static scenery otherwise pays per-frame call overhead
                // for objects that never move.
                Sort();
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            Sort();
        }

        private void Sort()
        {
            int order = Mathf.RoundToInt(-sortPoint.position.y * multiplier) + offset;
            if (order == _lastOrder) return;

            _lastOrder = order;
            if (sg != null)
                sg.sortingOrder = order;
            else if (sr != null)
                sr.sortingOrder = order;
        }

#if UNITY_EDITOR
        // Visualize the sort line in the Scene view.
        private void OnDrawGizmosSelected()
        {
            Transform p = sortPoint != null ? sortPoint : transform;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(p.position + Vector3.left * 0.5f, p.position + Vector3.right * 0.5f);
            Gizmos.DrawSphere(p.position, 0.03f);
        }
#endif
    }
}
