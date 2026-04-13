using System;

namespace DwarfsCrypt.Domain.Characters
{
    [Serializable]
    public struct CharacterStats
    {
        public int Str;
        public int Dex;
        public int Con;
        public int Wit;
        public int Men;
        public int Luc;

        public CharacterStats(int str, int dex, int con, int wit, int men, int luc)
        {
            Str = str;
            Dex = dex;
            Con = con;
            Wit = wit;
            Men = men;
            Luc = luc;
        }
    }
}
