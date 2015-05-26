using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;

namespace UniversalLeveler
{
    public class UdyrStrategy : ALevelStrategy
    {
        private readonly IDictionary<SpellSlot, int> _minimumLevelBySpellSlot = new Dictionary<SpellSlot, int>
        {
            { SpellSlot.Q, 4 },
            { SpellSlot.W, 4 },
            { SpellSlot.E, 4 },
            { SpellSlot.R, 4 },
        };

        public override int LevelOneSkills
        {
            get { return 0; }
        }

        public override int MinimumLevel(SpellSlot spellSlot)
        {
            return _minimumLevelBySpellSlot[spellSlot];
        }

        public override SpellSlot GetSpellSlotToLevel(int currentLevel, SpellSlot[] priorities, bool ignoreBaseLevel)
        {
            foreach (SpellSlot s in priorities)
            {
                bool baselevel = ignoreBaseLevel || ((ObjectManager.Player.Spellbook.GetSpell(s).Level == 0 && currentLevel <= 4) ||
                                  currentLevel > 4);
                if (baselevel && currentLevel >= Program.GetMinLevel(s) && CanLevel(currentLevel, s))
                {
                    return s;
                }
            }
            return SpellSlot.Unknown;
        }
    }
}
