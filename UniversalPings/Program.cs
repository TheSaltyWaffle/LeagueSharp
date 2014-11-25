using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UniversalPings
{
    internal class Program
    {
        private static Program _instance;
        private readonly IList<Ping> _pings = new List<Ping>();
        private Menu _menu;

        private static void Main(string[] args)
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
            _menu.AddItem(new MenuItem("print", "Show").SetValue(new StringList(new[] { "Champion", "Hero", "Both" })));
            _menu.AddItem(new MenuItem("block", "[Block Settings]"));
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.Team == ObjectManager.Player.Team && hero.NetworkId != ObjectManager.Player.NetworkId)
                {
                    _menu.AddItem(new MenuItem(hero.Name,
                        hero.Name + " (" + hero.ChampionName + ")"))
                        .SetValue(false);
                }
            }
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
                    MenuItem blockItem = _menu.Item(src.Name);
                    if (blockItem != null && blockItem.GetValue<bool>())
                    {
                        args.Process = false;
                        return;
                    }
                    GameObject target = ObjectManager.GetUnitByNetworkId<GameObject>(decoded.TargetNetworkId);
                    if (decoded.Type == Packet.PingType.OnMyWay || !src.IsValid) return;
                    Color c = Color.White;
                    switch (decoded.Type)
                    {
                        case Packet.PingType.Fallback:
                        case Packet.PingType.EnemyMissing:
                            c = Color.LightYellow;
                            break;
                        case Packet.PingType.AssistMe:
                            c = Color.LightBlue;
                            break;
                        case Packet.PingType.Danger:
                            c = new Color(255, 204, 203);
                            break;
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
                    _pings.Add(new Ping(name, Game.ClockTime + 2f, decoded.X, decoded.Y, target, c));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Print(string msg)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Universal</font><font color='#BABABA'>Pings:</font> <font color='#FFFFFF'>" + msg +
                "</font>");
        }
    }

    internal class Ping : Render.Text
    {
        public Ping(String name, float end, float x, float y, GameObject target, Color c)
            : base(name, 0, 0, 20, c)
        {
            End = end;
            Target = target;
            Loc = new Vector2(x, y);
            Centered = true;
            OutLined = true;
            PositionUpdate = delegate
            {
                if (Target.NetworkId != 0)
                {
                    return Drawing.WorldToScreen(Target.Position);
                }
                return
                    Drawing.WorldToScreen(new Vector3(x, y,
                        NavMesh.GetHeightForPosition(x, y)));
            };
        }

        public float End { get; set; }
        public GameObject Target { get; set; }
        public Vector2 Loc { get; set; }
    }
}