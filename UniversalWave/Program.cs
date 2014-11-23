using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UniversalWave
{
    class Program
    {
        private static IList<Minion> Minions = new List<Minion>();

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += GameOnOnGameLoad;
        }

        private static void GameOnOnGameLoad(EventArgs args)
        {
            if (Utility.Map.GetMap()._MapType == Utility.Map.MapType.SummonersRift)
            {
                Game.OnGameUpdate += Game_OnGameUpdate;
                GameObject.OnCreate += GameObjectOnOnCreate;
                Drawing.OnDraw += Drawing_OnDraw;
                Game.PrintChat("Loaded");
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            foreach (Minion m in Minions)
            {

            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Minion m in Minions)
            {
                Utility.DrawCircle(m.Position, 20, System.Drawing.Color.Green, 5, 30, true);
            }

        }

        private static void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsValid && sender is Obj_AI_Minion)
            {
                Obj_AI_Minion minion = (Obj_AI_Minion)sender;
                if (minion.Team == ObjectManager.Player.Team)
                {
                    Minion m = new Minion(minion);
                    Minions.Add(m);
                    Game.PrintChat(m.Team + " " + m.MoveSpeed + " " + m.Lane);
                }
            }
        }
    }


    public class Minion
    {

        public Vector3 Position { get; set; }
        public GameObjectTeam Team { get; set; }
        public float MoveSpeed { get; set; }
        public Lane Lane { get; set; }

        public Minion(Obj_AI_Minion minion)
        {
            Position = minion.Position;
            Team = minion.Team;
            MoveSpeed = minion.MoveSpeed;
            Match m = Regex.Match(minion.Name, @"Minion_T([0-9]+)L([0-9]+)S([0-9]+)N([0-9]+)");
            int team = Int32.Parse(m.Groups[1].Value);
            int lane = Int32.Parse(m.Groups[2].Value);
            int wave = Int32.Parse(m.Groups[3].Value);
            int nr = Int32.Parse(m.Groups[4].Value);
            Lane = (Lane)lane;
        }
    }

    public enum Lane
    {
        Top = 0,
        Middle = 1,
        Bot = 2
    }
}
