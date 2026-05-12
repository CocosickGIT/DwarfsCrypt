using UnityEngine;
using UnityEngine.Rendering;

namespace Presentation.Features
{
    public class YSorting : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sr;
        [SerializeField] private SortingGroup sg;
        [SerializeField] private int offset = 0;
        [SerializeField] private float multiplier = 1f;

        [SerializeField] private Transform _transform;

        private void Awake()
        {
            if (sr == null)
            {
                sr = GetComponent<SpriteRenderer>();
                sg = GetComponent<SortingGroup>();
            }
        }

        private void LateUpdate()
        {
            Sort();
        }

        private void Sort()
        {
            int order = (int)(-_transform.position.y * multiplier) + offset;
            if (sr != null)
                sr.sortingOrder = order;
            else if (sg != null)
                sg.sortingOrder = order;
        }
    }
}
