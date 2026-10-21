using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    // A single floating combat-damage number that pops in, rises while fading out, then signals
    // that it is finished so it can be reused. Instances are pooled by DamagePopupPool — building
    // a TextMeshPro at runtime is expensive and destroying it churns the GC, so heavy combat reuses
    // a handful of these instead of allocating one per hit.
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopup : MonoBehaviour
    {
        private TextMeshPro _text;
        private MeshRenderer _meshRenderer;
        private Color _baseColor;
        private float _lifetime;
        private float _riseSpeed;
        private Vector3 _drift;

        /// <summary>Raised when the animation completes. The pool listens to reclaim this instance.</summary>
        public event Action<DamagePopup> Finished;

        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.autoSizeTextContainer = true;
            _text.fontStyle = FontStyles.Bold;
        }

        public void Play(
            string label, Color color, float fontSize,
            float lifetime, float riseSpeed, float horizontalDrift,
            string sortingLayerName, int sortingOrder, TMP_FontAsset font)
        {
            if (font != null && _text.font != font) _text.font = font;
            _text.text = label;
            _text.color = color;
            _text.fontSize = fontSize;

            _meshRenderer.sortingLayerName = sortingLayerName;
            _meshRenderer.sortingOrder = sortingOrder;

            _baseColor = color;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _riseSpeed = riseSpeed;
            _drift = new Vector3(horizontalDrift, 0f, 0f);

            transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;

            while (elapsed < _lifetime)
            {
                float t = elapsed / _lifetime;

                transform.position += (Vector3.up * _riseSpeed + _drift) * Time.deltaTime;

                // Quick pop-in over the first 15% of life, then settle.
                float scale = t < 0.15f ? Mathf.Lerp(0.6f, 1f, t / 0.15f) : 1f;
                transform.localScale = new Vector3(scale, scale, scale);

                // Hold full opacity for the first half, then fade out over the second.
                float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) / 0.5f;
                _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Finished?.Invoke(this);
        }
    }
}
