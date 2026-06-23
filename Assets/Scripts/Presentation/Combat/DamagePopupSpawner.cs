using TMPro;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    // Add to any character prefab alongside CharacterComponent (same pattern as HitFlashEffect).
    // Every time the character takes damage it spawns a floating "-damage" number in world space.
    // Critical hits use a larger, red number; normal hits use the normal color/size.
    [RequireComponent(typeof(CharacterComponent))]
    public class DamagePopupSpawner : MonoBehaviour
    {
        [Header("Spawn Position")]
        [Tooltip("World-space offset above the character where numbers appear.")]
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 1.5f, 0f);
        [Tooltip("Random horizontal scatter so rapid stacked hits don't overlap exactly.")]
        [SerializeField] private float _spawnScatter = 0.3f;

        [Header("Normal Hit")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private float _normalFontSize = 4f;

        [Header("Critical Hit")]
        [Tooltip("Crit damage shows in a slightly bigger, red number.")]
        [SerializeField] private Color _critColor = new Color(1f, 0.2f, 0.15f);
        [SerializeField] private float _critFontSize = 5.6f;

        [Header("Animation")]
        [SerializeField] private float _lifetime = 0.8f;
        [SerializeField] private float _riseSpeed = 1.5f;
        [SerializeField] private float _horizontalDrift = 0.3f;

        [Header("Rendering")]
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _sortingOrder = 300;
        [Tooltip("Optional. Leave empty to use the TMP default font asset.")]
        [SerializeField] private TMP_FontAsset _font;

        private CharacterComponent _character;

        private void Awake()
        {
            _character = GetComponent<CharacterComponent>();
            if (_character != null)
                _character.OnDamageTaken += HandleDamageTaken;
        }

        private void OnDestroy()
        {
            if (_character != null)
                _character.OnDamageTaken -= HandleDamageTaken;
        }

        private void HandleDamageTaken(float amount, bool isCrit)
        {
            int rounded = Mathf.Max(1, Mathf.RoundToInt(amount));
            string label = $"-{rounded}";

            Color color  = isCrit ? _critColor : _normalColor;
            float size   = isCrit ? _critFontSize : _normalFontSize;
            float drift  = Random.Range(-_horizontalDrift, _horizontalDrift);

            Vector3 scatter  = new Vector3(Random.Range(-_spawnScatter, _spawnScatter), 0f, 0f);
            // Spawn unparented and in world space so the character's facing-flip
            // (negative localScale.x) never mirrors the text.
            Vector3 worldPos = transform.position + _spawnOffset + scatter;

            var go = new GameObject("DamagePopup");
            go.transform.position = worldPos;

            go.AddComponent<TextMeshPro>();
            var popup = go.AddComponent<DamagePopup>();
            popup.Play(label, color, size, _lifetime, _riseSpeed, drift,
                       _sortingLayerName, _sortingOrder, _font);
        }
    }
}
