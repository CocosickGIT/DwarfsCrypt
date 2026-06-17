namespace DwarfsCrypt.Domain.Items
{
    /// <summary>Aggregated stat bonuses contributed by a set of equipped items.</summary>
    public struct EquipmentBonuses
    {
        public int Str;
        public int Dex;
        public int Con;
        public int Wit;
        public int Men;
        public int Luc;
        public int MaxHp;
        public int MaxStamina;
        public int MaxMana;

        public void Add(ItemData data)
        {
            if (data == null) return;
            Str += data.BonusStr;
            Dex += data.BonusDex;
            Con += data.BonusCon;
            Wit += data.BonusWit;
            Men += data.BonusMen;
            Luc += data.BonusLuc;
            MaxHp += data.BonusMaxHp;
            MaxStamina += data.BonusMaxStamina;
            MaxMana += data.BonusMaxMana;
        }
    }
}
