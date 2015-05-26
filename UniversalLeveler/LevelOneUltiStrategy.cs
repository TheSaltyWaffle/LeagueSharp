using System.Collections.Generic;
using LeagueSharp;

namespace UniversalLeveler
{
    public class LevelOneUltiStrategy : ALevelStrategy
    {
        private readonly IDictionary<SpellSlot, int> _minimumLevelBySpellSlot = new Dictionary<SpellSlot, int>
        {
            { SpellSlot.Q, 3 },
            { SpellSlot.W, 3 },
            { SpellSlot.E, 3 },
            { SpellSlot.R, 6 },
        };

        public override int LevelOneSkills
        {
            get { return 1; }
        }

        public override int MinimumLevel(SpellSlot spellSlot)
        {
            return _minimumLevelBySpellSlot[spellSlot];
        }

        public override bool CanLevel(int currentLevel, SpellSlot spellSlot)
        {
            if (spellSlot == SpellSlot.R)
            {
                return base.CanLevel(currentLevel, spellSlot) && CanLevelUlti(currentLevel, 1, spellSlot);
            }
            return base.CanLevel(currentLevel, spellSlot);
        }

        public override SpellSlot GetSpellSlotToLevel(int currentLevel, SpellSlot[] priorities, bool ignoreBaseLevel)
        {
            foreach (SpellSlot s in priorities)
            {
                bool baselevel = ignoreBaseLevel || ((ObjectManager.Player.Spellbook.GetSpell(s).Level == 0 && currentLevel <= 3) ||
                                  currentLevel > 3);
                if (baselevel && currentLevel >= Program.GetMinLevel(s) && CanLevel(currentLevel, s))
                {
                    return s;
                }
            }
            return SpellSlot.Unknown;
        }
    }
}