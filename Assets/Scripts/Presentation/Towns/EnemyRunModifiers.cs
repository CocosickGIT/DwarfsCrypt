using Core.Towns;
using DwarfsCrypt.Domain.Characters;
using DwarfsCrypt.Domain.Towns;
using DwarfsCrypt.Presentation.Combat;
using DwarfsCrypt.Presentation.Enemy;

namespace DwarfsCrypt.Presentation.Towns
{
    /// <summary>
    /// Applies the active run's <see cref="TownModifier"/>s to a freshly spawned enemy. Called once
    /// per spawn by <see cref="Spawner.CharacterSpawner"/>; a no-op when no run context is set (e.g.
    /// in town). Modifier values are multipliers (1.3 = +30%); a missing/unknown type is ignored.
    ///
    /// New modifier types plug in here — mirrors the CreateEffect switches in SkillCaster/BoosterController.
    /// </summary>
    public static class EnemyRunModifiers
    {
        private const string Source = "town";

        public static void Apply(CharacterComponent character, EnemyController controller)
        {
            var mods = RunContext.Modifiers;
            if (mods == null || mods.Count == 0) return;

            var c = character != null ? character.Character : null;
            if (c == null) return;

            bool hpChanged = false;

            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                if (m == null || string.IsNullOrEmpty(m.Type)) continue;

                switch (m.Type)
                {
                    case "enemy_hp_mult":
                        c.Attributes.AddModifier(new StatModifier(
                            AttributeType.MaxHp, ModifierType.PercentFinal, m.Value - 1f, Source));
                        hpChanged = true;
                        break;

                    case "enemy_damage_mult":
                        c.Attributes.AddModifier(new StatModifier(
                            AttributeType.PhysicalAttack, ModifierType.PercentFinal, m.Value - 1f, Source));
                        break;

                    case "enemy_speed_mult":
                        controller?.SetMoveSpeedMultiplier(m.Value);
                        break;
                }
            }

            // MaxHp changed after the enemy was built (CurrentHp was set from the old max at
            // construction), so top it up to the new full pool.
            if (hpChanged) c.RefillVitals();
        }
    }
}
