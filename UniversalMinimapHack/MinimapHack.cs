using System.Collections.Generic;
using System.Linq;
using LeagueSharp;

namespace UniversalMinimapHack
{
    public class MinimapHack
    {
        private static readonly MinimapHack MinimapHackInstance = new MinimapHack();

        private readonly IList<HeroTracker> _heroTrackers = new List<HeroTracker>();

        public Menu Menu { get; private set; }

        public static MinimapHack Instance()
        {
            return MinimapHackInstance;
        }

        public void Load()
        {
            Menu = new Menu();
            foreach (Obj_AI_Hero hero in
                ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team))
            {
                _heroTrackers.Add(new HeroTracker(hero, ImageLoader.Load(hero.ChampionName)));
            }
        }
    }
}