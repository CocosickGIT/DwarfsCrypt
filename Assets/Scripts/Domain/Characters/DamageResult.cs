namespace DwarfsCrypt.Domain.Characters
{
    // Result of a damage roll: the amount dealt and whether it was a critical hit.
    // Carried through the combat pipeline so the presentation layer can show
    // distinct floating numbers for normal vs. crit hits.
    public readonly struct DamageResult
    {
        public readonly float Amount;
        public readonly bool IsCrit;

        public DamageResult(float amount, bool isCrit)
        {
            Amount = amount;
            IsCrit = isCrit;
        }

        public static DamageResult operator *(DamageResult result, float multiplier) =>
            new DamageResult(result.Amount * multiplier, result.IsCrit);
    }
}
