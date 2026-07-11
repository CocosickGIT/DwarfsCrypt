using UnityEngine;
using DwarfsCrypt.Domain.Boosters;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Presentation.Player;
using DwarfsCrypt.Presentation.Combat;

namespace DwarfsCrypt.Presentation.Boosters
{
    public class BoosterController : MonoBehaviour
    {
        [SerializeField] private BoosterEffectType _effectType = BoosterEffectType.StrengthBuff;
        [SerializeField] private float _effectValue = 10f;
        [SerializeField] private float _interactRadius = 2f;
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private GameObject _sprite;
        [Tooltip("Icon shown in the on-screen buff bar while this shrine's effect is active.")]
        [SerializeField] private Sprite _buffIcon;
        
        private BoosterEffect _effect;
        private GameHUD _hud;
        private CharacterComponent _playerCharacter;
        private bool _playerInRange;
        private bool _consumed;

        private void Start()
        {
            _effect = CreateEffect(_effectType, _effectValue);
            _hud = FindObjectsByType<GameHUD>(FindObjectsSortMode.None)[0];
            _hud.OnInteractPressed += OnInteract;
        }

        private void OnDestroy()
        {
            if (_hud != null)
                _hud.OnInteractPressed -= OnInteract;
        }

        private void Update()
        {
            if (_consumed) return;

            Collider2D hit = Physics2D.OverlapCircle(transform.position, _interactRadius, _playerLayer);
            bool inRange = hit != null;

            if (inRange == _playerInRange) return;

            _playerInRange = inRange;
            _playerCharacter = inRange
                ? (hit.GetComponentInChildren<CharacterComponent>() ?? hit.GetComponent<CharacterComponent>())
                : null;

            _hud?.SetInteractVisible(inRange);
        }

        private void OnInteract()
        {
            if (!_playerInRange || _consumed || _playerCharacter == null) return;

            _consumed = true;
            _hud.SetInteractVisible(false);

            // Apply the effect, then hand the timing to the BuffTracker so the UI can mirror it
            // and the modifier is removed centrally when the timer runs out.
            var effect = _effect;
            CharacterAttributes attributes = _playerCharacter.Attributes;
            effect.Apply(attributes);
            Debug.Log($"{effect} " + "is applied");

            BuffTracker.Instance.Add(
                effect.Source, effect.DisplayName, _buffIcon, effect.Duration,
                onExpire: () => effect.Remove(attributes));

            HideObject();
        }

        private void HideObject() //TODO fix, hide whole obj, make an additional obj that would be hided 
        {
             _sprite.SetActive(false);
            // foreach (var rend in GetComponentsInChildren<Renderer>())
            //     rend.enabled = false;
            //
            // foreach (var col in GetComponentsInChildren<Collider2D>())
            //     col.enabled = false;
        }

        private static BoosterEffect CreateEffect(BoosterEffectType type, float value) => type switch
        {
            BoosterEffectType.StrengthBuff => new StrengthBoosterEffect(value),
            _ => throw new System.ArgumentOutOfRangeException(nameof(type))
        };

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
    }
}
