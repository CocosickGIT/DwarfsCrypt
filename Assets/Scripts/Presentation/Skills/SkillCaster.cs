using System;
using UnityEngine;
using DwarfsCrypt.Domain.Skills;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Player;
using Core.Skills;

namespace DwarfsCrypt.Presentation.Skills
{
    /// <summary>
    /// Casts the player's single active skill when the SkillController fires. The skill is chosen in
    /// the inspector from a dropdown of ids in skills.json (see <see cref="SkillIdAttribute"/>); its
    /// numbers come from that JSON and its behavior from the resolved <see cref="ISkillEffect"/>
    /// (Variant D split). Owns one cooldown timer and runs the effect.
    /// </summary>
    public class SkillCaster : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SkillController _skillController;
        [SerializeField] private PlayerController _player;
        [SerializeField] private MeleeAttackHitbox _hitbox;
        [SerializeField] private CharacterComponent _character;
        [Tooltip("HUD used to drive the skill cooldown overlay / mana label. Auto-found if left empty.")]
        [SerializeField] private GameHUD _hud;

        [Header("Config")]
        [Tooltip("Resources path to the skills config, no extension.")]
        [SerializeField] private string _skillsResourcePath = "Skills/skills";
        [Tooltip("How long the swing animation/hit window lasts, like the basic attack.")]
        [SerializeField] private float _attackDuration = 0.5f;

        [Header("Active skill")]
        [Tooltip("Which skill this player casts. Picked from the ids in skills.json.")]
        [SkillId] [SerializeField] private string _skillId = "wide_attack";

        private SkillConfig _skill;
        private float _cooldownRemaining;

        private void Start()
        {
            if (_skillController == null)
            {
                var controllers = FindObjectsByType<SkillController>(FindObjectsSortMode.None);
                if (controllers.Length > 0) _skillController = controllers[0];
            }

            if (_hud == null)
            {
                var huds = FindObjectsByType<GameHUD>(FindObjectsSortMode.None);
                if (huds.Length > 0) _hud = huds[0];
            }

            _skill = LoadSkill(_skillId);
            _hud?.SetSkillManaCost(_skill != null ? (int)_skill.ManaCost : 0);

            if (_skillController != null)
                _skillController.OnSkillUsed += Cast;
        }

        private void OnDestroy()
        {
            if (_skillController != null)
                _skillController.OnSkillUsed -= Cast;
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);
        }

        private SkillConfig LoadSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;

            SkillsCollection collection;
            try
            {
                collection = SkillConfigLoader.LoadFromResources(_skillsResourcePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkillCaster] Could not load skills config '{_skillsResourcePath}': {e.Message}");
                return null;
            }

            if (collection?.Skills == null) return null;

            foreach (var skill in collection.Skills)
                if (skill != null && skill.Id == skillId)
                    return skill;

            Debug.LogWarning($"[SkillCaster] No skill with id '{skillId}' in {_skillsResourcePath}.");
            return null;
        }

        private void Cast()
        {
            if (_skill == null) return;
            if (_character == null || _character.Character == null || _character.IsDead) return;
            if (_hitbox == null) return;

            // Still on cooldown.
            if (_cooldownRemaining > 0f) return;

            // Not enough mana — the cast fails and no cooldown is started.
            if (!_character.Character.TrySpendMana(_skill.ManaCost)) return;

            if (_skill.Cooldown > 0f)
            {
                _cooldownRemaining = _skill.Cooldown;
                _hud?.StartSkillCooldown(_skill.Cooldown);
            }

            Vector2 aim = _player != null ? _player.AimDirection : (Vector2)transform.right;

            var ctx = new SkillCastContext(_skill, _character.Character, _hitbox, aim, _attackDuration);

            // Reuse the player's attack animation for the swing.
            _player?.PlayAttackAnimation();

            ISkillEffect effect = CreateEffect(_skill.EffectType);
            effect.Execute(ctx);
        }

        // Maps the data-driven effect type to its behavior strategy. Mirrors
        // BoosterController.CreateEffect — add a case here when introducing a new SkillEffectType.
        private static ISkillEffect CreateEffect(SkillEffectType type) => type switch
        {
            SkillEffectType.MeleeArc => new MeleeArcSkillEffect(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled skill effect type"),
        };
    }
}
