using System;

namespace DwarfsCrypt.Domain.Characters
{
    // One telegraphed boss attack, authored in the character JSON (CharacterConfig.BossAttacks).
    // The zone is boss-centered: an arc of Radius / HalfAngle in the direction of the target,
    // locked the moment the boss commits. HalfAngle 180 = full circle.
    [Serializable]
    public class BossAttackPattern
    {
        public string Id;

        public float Windup = 1f;      // telegraph fill time; the player's dodge window
        public float Recovery = 0.5f;  // pause after the hit before the boss moves again
        public float Cooldown = 5f;    // per-pattern cooldown

        public float DamageMultiplier = 1f;
        public float Radius = 2f;
        public float HalfAngle = 180f;

        // Target-distance window for selecting this pattern. MaxRange <= 0 means no upper bound.
        public float MinRange;
        public float MaxRange;

        public float Weight = 1f;      // selection weight among currently valid patterns
        public bool EnrageOnly;        // only selectable while the boss is enraged
    }
}
