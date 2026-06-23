using System;
using System.Collections;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    // Add alongside CharacterComponent (same pattern as HitFlashEffect). When the
    // character dies it waits for the death animation, swaps every child SpriteRenderer
    // to a dissolve material and animates the dissolve 0 -> 1, then raises Dissolved so
    // the owner (e.g. CharacterSpawner) can return the unit to its pool.
    [RequireComponent(typeof(CharacterComponent))]
    public class DissolveOnDeath : MonoBehaviour
    {
        [Tooltip("DwarfsCrypt/SpriteDissolve. If left empty, resolved by name at runtime.")]
        [SerializeField] private Shader _dissolveShader;

        [Tooltip("The whole-unit GameObject to dissolve and remove. CharacterComponent often " +
                 "sits on a child (UnitRoot), so point this at the prefab root that holds the " +
                 "controller/Rigidbody. Falls back to this GameObject if left empty.")]
        [SerializeField] private GameObject _unitRoot;

        [Tooltip("Seconds to wait after death (lets the DEATH animation play) before dissolving.")]
        [SerializeField] private float _delayBeforeDissolve = 0.6f;
        [SerializeField] private float _dissolveDuration = 1.0f;

        [Header("Look")]
        [SerializeField] private float _noiseScale = 12f;
        [SerializeField, Range(0f, 0.5f)] private float _edgeWidth = 0.06f;
        [ColorUsage(true, true)]
        [SerializeField] private Color _edgeColor = new Color(2f, 0.9f, 0.25f, 1f);

        // Raised once the dissolve has finished. The owner returns the unit to its pool.
        // If nothing is listening the unit simply deactivates itself so the corpse still
        // leaves the scene.
        public event Action<DissolveOnDeath> Dissolved;

        public GameObject Unit => _unitRoot != null ? _unitRoot : gameObject;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int NoiseScaleId     = Shader.PropertyToID("_NoiseScale");
        private static readonly int EdgeWidthId      = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EdgeColorId      = Shader.PropertyToID("_EdgeColor");

        private CharacterComponent _character;
        private SpriteRenderer[] _renderers;
        private Material[] _originalMaterials;
        private Material _dissolveMat;
        private bool _dying;

        private void Awake()
        {
            _character = GetComponent<CharacterComponent>();
            if (_character != null)
                _character.OnDied += HandleDied;

            CaptureRenderers();
        }

        // Runs on every pool re-activation: make the unit whole and ready to die again.
        private void OnEnable()
        {
            RestoreOriginalMaterials();
            _dying = false;
        }

        private void OnDestroy()
        {
            if (_character != null)
                _character.OnDied -= HandleDied;
            if (_dissolveMat != null)
                Destroy(_dissolveMat);
        }

        private void CaptureRenderers()
        {
            _renderers = Unit.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
            _originalMaterials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _originalMaterials[i] = _renderers[i].sharedMaterial;
        }

        private void RestoreOriginalMaterials()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null)
                    _renderers[i].sharedMaterial = _originalMaterials[i];
        }

        private void HandleDied()
        {
            if (_dying) return;
            _dying = true;
            StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveRoutine()
        {
            if (_delayBeforeDissolve > 0f)
                yield return new WaitForSeconds(_delayBeforeDissolve);

            // Shader missing — skip the visual but still hand the unit back for removal.
            if (!EnsureDissolveMaterial())
            {
                Finish();
                yield break;
            }

            // One shared material instance drives every sprite part; the sprite texture
            // itself is per-renderer data so each part keeps showing its own art.
            _dissolveMat.SetFloat(DissolveAmountId, 0f);
            if (_renderers != null)
                foreach (var sr in _renderers)
                    if (sr != null) sr.sharedMaterial = _dissolveMat;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _dissolveDuration);
            while (elapsed < duration)
            {
                _dissolveMat.SetFloat(DissolveAmountId, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _dissolveMat.SetFloat(DissolveAmountId, 1f);

            Finish();
        }

        private bool EnsureDissolveMaterial()
        {
            if (_dissolveMat != null) return true;

            Shader shader = _dissolveShader != null
                ? _dissolveShader
                : Shader.Find("DwarfsCrypt/SpriteDissolve");
            if (shader == null) return false;

            _dissolveMat = new Material(shader);
            _dissolveMat.SetFloat(NoiseScaleId, _noiseScale);
            _dissolveMat.SetFloat(EdgeWidthId, _edgeWidth);
            _dissolveMat.SetColor(EdgeColorId, _edgeColor);
            return true;
        }

        private void Finish()
        {
            // Owner (CharacterSpawner) deactivates + pools the unit; materials are restored
            // on the next OnEnable. With no owner, deactivate so the corpse still vanishes.
            if (Dissolved != null)
                Dissolved.Invoke(this);
            else
                Unit.SetActive(false);
        }
    }
}
