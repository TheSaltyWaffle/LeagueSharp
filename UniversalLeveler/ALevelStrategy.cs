using System.Collections.Generic;
using LeagueSharp;

namespace UniversalLeveler
{
    public abstract class ALevelStrategy
    {
        private readonly IDictionary<int, int> _summonerLevelByUltiLevel = new Dictionary<int, int>
        {
            { 1, 6 },
            { 2, 11 },
            { 3, 16 },
        };

        public abstract int LevelOneSkills { get; }

        public abstract int MinimumLevel(SpellSlot spellSlot);

        public abstract SpellSlot GetSpellSlotToLevel(int currentLevel, SpellSlot[] priorities, bool ignoreBaseLevel);

        public virtual bool CanLevel(int currentLevel, SpellSlot spellSlot)
        {
            int spellLevel = ObjectManager.Player.Spellbook.GetSpell(spellSlot).Level;
            if (spellLevel >= 5)
            {
                return false;
            }
            int div = currentLevel / 2;
            if (((currentLevel ^ 2) >= 0) && (currentLevel % 2 != 0))
            {
                div++;
            }
            return spellLevel < div;
        }

        protected bool CanLevelUlti(int currentLevel, int defaultLevel, SpellSlot spellSlot)
        {
            int levelUlti = ObjectManager.Player.Spellbook.GetSpell(spellSlot).Level - defaultLevel;
            if (levelUlti >= 3)
            {
                return false;
            }
            return currentLevel >= _summonerLevelByUltiLevel[levelUlti + 1];
        }
    }
}