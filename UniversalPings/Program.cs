using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UniversalPings
{
    class Program
    {
        private static Program _instance;
        private readonly IList<Ping> _pings = new List<Ping>();
        private Menu _menu;

        static void Main(string[] args)
        {
            _instance = new Program();
        }

        public Program()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        ~Program()
        {
            Game.OnGameUpdate -= GameOnOnGameUpdate;
            Drawing.OnEndScene -= Drawing_OnEndScene;
            Drawing.OnPreReset -= Drawing_OnPreReset;
            Drawing.OnPostReset -= Drawing_OnPostReset;
            Game.OnGameProcessPacket -= Game_OnGameProcessPacket;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            _menu = new Menu("Universal Pings", "UniversalPings", true);
            _menu.AddItem(new MenuItem("print", "Print").SetValue(new StringList(new[] { "Champion", "Hero", "Both" })));
            _menu.AddToMainMenu();

            Game.OnGameUpdate += GameOnOnGameUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Print("Loaded!");
        }

        private void Drawing_OnPostReset(EventArgs args)
        {
            foreach (Ping p in _pings)
            {
                p.OnPostReset();
            }
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            foreach (Ping p in _pings)
            {
                p.OnPreReset();
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            foreach (Ping p in _pings)
            {
                p.OnEndScene();
            }
        }

        private void GameOnOnGameUpdate(EventArgs args)
        {
            for (int i = _pings.Count - 1; i >= 0; i--)
            {
                Ping p = _pings[i];
                if (Game.ClockTime > p.End)
                {
                    p.Dispose();
                    _pings.RemoveAt(i);
                }
            }
        }

        private void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            try
            {
                if (args.PacketData[0] == Packet.S2C.Ping.Header)
                {
                    Packet.S2C.Ping.Struct decoded = Packet.S2C.Ping.Decoded(args.PacketData);
                    Obj_AI_Hero src = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(decoded.SourceNetworkId);
                    if (decoded.Type != Packet.PingType.OnMyWay && src != null && src.IsValid)
                    {
                        Color c = Color.White;
                        if (decoded.Type == Packet.PingType.EnemyMissing || decoded.Type == Packet.PingType.Fallback)
                        {
                            c = Color.LightYellow;
                        }
                        else if (decoded.Type == Packet.PingType.AssistMe)
                        {
                            c = Color.LightBlue;
                        }
                        else if (decoded.Type == Packet.PingType.Danger)
                        {
                            c = new Color(255, 204, 203);
                        }

                        int selectedIndex = _menu.Item("print").GetValue<StringList>().SelectedIndex;
                        String name;
                        switch (selectedIndex)
                        {
                            case 0:
                                name = src.ChampionName;
                                break;
                            case 1:
                                name = src.Name;
                                break;
                            default:
                                name = src.Name + " (" + src.ChampionName + ")";
                                break;
                        }
                        _pings.Add(new Ping(name, Game.ClockTime + 2f, new Vector2(decoded.X, decoded.Y), c));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        private void Print(string msg)
        {
            Game.PrintChat("<font color='#ff3232'>Universal</font><font color='#BABABA'>Pings:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }
    }

    class Ping : Render.Text
    {
        public Ping(String name, float end, Vector2 loc, Color c)
            : base(name, 0, 0, 20, c)
        {
            End = end;
            Loc = loc;
            Centered = true;
            OutLined = true;
            PositionUpdate = () => Drawing.WorldToScreen(new Vector3(Loc, NavMesh.GetHeightForPosition(loc.X, loc.Y)));
        }

        public float End { get; set; }
        public Vector2 Loc { get; set; }

    }
}
