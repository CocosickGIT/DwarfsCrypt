namespace DwarfsCrypt.Domain.Characters
{
    public interface IDamageable
    {
        void TakeDamage(float damage, bool isCrit = false);
    }
}
