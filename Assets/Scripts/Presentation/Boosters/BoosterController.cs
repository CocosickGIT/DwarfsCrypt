using System.Collections;
using UnityEngine;
using DwarfsCrypt.Domain.Boosters;
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
                ? (hit.GetComponentInParent<CharacterComponent>() ?? hit.GetComponent<CharacterComponent>())
                : null;

            _hud?.SetInteractVisible(inRange);
        }

        private void OnInteract()
        {
            if (!_playerInRange || _consumed || _playerCharacter == null) return;

            _consumed = true;
            _hud.SetInteractVisible(false);

            _effect.Apply(_playerCharacter.Attributes);
            StartCoroutine(RemoveEffectCoroutine(_playerCharacter));

            HideObject();
        }

        private IEnumerator RemoveEffectCoroutine(CharacterComponent character)
        {
            yield return new WaitForSeconds(_effect.Duration);
            _effect.Remove(character.Attributes);
            // Destroy(gameObject);
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
