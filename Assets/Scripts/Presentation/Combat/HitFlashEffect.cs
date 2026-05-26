using System.Collections;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    // Add to any character prefab alongside CharacterComponent.
    // Flashes all child SpriteRenderers white on hit, without mutating shared materials.
    public class HitFlashEffect : MonoBehaviour
    {
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashDuration = 0.12f;

        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        private SpriteRenderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Coroutine _activeFlash;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            _block = new MaterialPropertyBlock();

            var character = GetComponent<CharacterComponent>();
            if (character != null)
                character.OnDamaged += OnDamaged;
        }

        private void OnDestroy()
        {
            var character = GetComponent<CharacterComponent>();
            if (character != null)
                character.OnDamaged -= OnDamaged;
        }

        private void OnDamaged(float _currentHp, float _maxHp) => Flash();

        public void Flash()
        {
            if (_activeFlash != null)
                StopCoroutine(_activeFlash);
            _activeFlash = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            ApplyColor(_flashColor);
            yield return new WaitForSeconds(_flashDuration);
            ApplyColor(Color.white);
            _activeFlash = null;
        }

        private void ApplyColor(Color color)
        {
            _block.SetColor(ColorProp, color);
            foreach (var sr in _renderers)
                sr.SetPropertyBlock(_block);
        }
    }
}
