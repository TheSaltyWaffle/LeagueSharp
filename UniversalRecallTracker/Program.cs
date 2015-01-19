using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace UniversalRecallTracker
{
    public class Program
    {
        private readonly IDictionary<Obj_AI_Hero, RecallInfo> _recallInfo = new Dictionary<Obj_AI_Hero, RecallInfo>();

        private static Program _instance;

        private MenuItem _y;
        private MenuItem _x;
        private MenuItem _textSize;
        private MenuItem _chatWarning;
        private MenuItem _barScale;

        public int X
        {
            get { return _x.GetValue<Slider>().Value; }
        }

        public int Y
        {
            get { return _y.GetValue<Slider>().Value; }
        }

        public int TextSize
        {
            get { return _textSize.GetValue<Slider>().Value; }
        }

        public float BarScale
        {
            get { return _barScale.GetValue<Slider>().Value / 100f; }
        }

        public bool ChatWarning
        {
            get { return _chatWarning.GetValue<bool>(); }
        }

        public static void Main(string[] args)
        {
            new Program();
        }

        private Program()
        {
            _instance = this;
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        public static Program Instance()
        {
            if (_instance == null)
            {
                return new Program();
            }
            return _instance;
        }

        public void Game_OnGameLoad(EventArgs args)
        {
            Menu menu = new Menu("Universal RecallTracker", "universalrecalltracker", true);
            _x =
                new MenuItem("x", "X").SetValue(
                    new Slider(
                        (int) ((Drawing.Direct3DDevice.Viewport.Width - Properties.Resources.RecallBar.Width) / 2f), 0,
                        Drawing.Direct3DDevice.Viewport.Width));
            _x.ValueChanged += _x_ValueChanged;
            _y =
                new MenuItem("y", "Y").SetValue(
                    new Slider(
                        (int) (Drawing.Direct3DDevice.Viewport.Height * 3f / 4f), 0,
                        Drawing.Direct3DDevice.Viewport.Height));
            _y.ValueChanged += _x_ValueChanged;
            _textSize = new MenuItem("textSize", "Text Size (F5 Reload)").SetValue(new Slider(15, 5, 50));
            _chatWarning = new MenuItem("chatWarning", "Chat Notification").SetValue(false);
            _barScale = new MenuItem("barScale", "Bar Scale %").SetValue(new Slider(100, 0, 200));
            _barScale.ValueChanged += _barScale_ValueChanged;

            menu.AddItem(_x);
            menu.AddItem(_y);
            menu.AddItem(_textSize);
            menu.AddItem(_barScale);
            menu.AddItem(_chatWarning);
            menu.AddToMainMenu();

            int i = 0;
            foreach (Obj_AI_Hero hero in
                ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.Team != ObjectManager.Player.Team))
            {
                RecallInfo recallInfo = new RecallInfo(hero, i++);
                _recallInfo[hero] = recallInfo;
            }
            //_recallInfo[ObjectManager.Player] = new RecallInfo(ObjectManager.Player,i);
            Print("Loaded!");
        }

       private void _barScale_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            foreach (RecallInfo info in _recallInfo.Values)
            {
                info.Scale(BarScale);
            }
        }

        private void _x_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            foreach (RecallInfo info in _recallInfo.Values)
            {
                info.Reset();
            }
        }

        public void Print(string msg, bool timer = false)
        {
            string s = null;
            if (timer)
            {
                s = "<font color='#d8d8d8'>[" + Utils.FormatTime(Game.ClockTime) + "]</font> ";
            }
            s +=
                "<font color='#ff3232'>Universal</font><font color='#d4d4d4'>RecallTracker:</font> <font color='#FFFFFF'>" +
                msg + "</font>";
            Game.PrintChat(s);
        }

        public void Notify(string msg)
        {
            if (ChatWarning)
            {
                Print(msg, true);
            }
        }
    }

    public class RecallInfo
    {
        public const int GapTextBar = 10;

        private static readonly Font TextFont = new Font(
            Drawing.Direct3DDevice,
            new FontDescription
            {
                FaceName = "Calibri",
                Height = Program.Instance().TextSize,
                OutputPrecision = FontPrecision.Default,
                Quality = FontQuality.Default,
            });

        private readonly Obj_AI_Hero _hero;
        private int _duration;
        private float _begin;
        private bool _active;
        private readonly int _index;

        private readonly Render.Sprite _sprite;
        private readonly Render.Text _countdownText;
        private int lastChange;

        public RecallInfo(Obj_AI_Hero hero, int index)
        {
            _hero = hero;
            _index = index;
            _sprite = new Render.Sprite(Properties.Resources.RecallBar, new Vector2(0, 0))
            {
                Scale = new Vector2(Program.Instance().BarScale,Program.Instance().BarScale),
                VisibleCondition = sender => _active || Environment.TickCount - lastChange < 3000,
                PositionUpdate =
                    () =>
                        new Vector2(Program.Instance().X, Program.Instance().Y - (_index * TextFont.Description.Height))
            };
            _sprite.Add(0);
            Render.Text heroText = new Render.Text(0, 0, hero.ChampionName, TextFont.Description.Height, Color.White)
            {
                OutLined = true,
                VisibleCondition = sender => _active || Environment.TickCount - lastChange < 3000,
                PositionUpdate = delegate
                {
                    Rectangle rect = TextFont.MeasureText(null, hero.ChampionName, 0);
                    return new Vector2(_sprite.X - rect.Width - GapTextBar, _sprite.Y - rect.Height / 2 + (_sprite.Height * Program.Instance().BarScale)/2);
                }
            };

            heroText.Add(1);
            _countdownText = new Render.Text(0, 0, "", TextFont.Description.Height, Color.White)
            {
                OutLined = true,
                VisibleCondition = sender => _active
            };
            _countdownText.Add(1);
            Game.OnGameUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
        }

        private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
            if (decoded.UnitNetworkId == _hero.NetworkId && decoded.Type == Packet.S2C.Teleport.Type.Recall)
            {
                switch (decoded.Status)
                {
                    case Packet.S2C.Teleport.Status.Start:
                        _begin = Game.ClockTime;
                        _duration = decoded.Duration;
                        _active = true;
                        break;
                    case Packet.S2C.Teleport.Status.Finish:
                        Program.Instance().Notify(_hero.ChampionName + " has recalled.");
                        _active = false;
                        break;
                    case Packet.S2C.Teleport.Status.Abort:
                        _active = false;
                        break;
                    case Packet.S2C.Teleport.Status.Unknown:
                        Program.Instance()
                            .Notify(
                                _hero.ChampionName + " is <font color='#ff3232'>unknown</font> (" +
                                _hero.Spellbook.GetSpell(SpellSlot.Recall).Name + ")");
                        _active = false;
                        break;
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (_active && _duration > 0)
            {
                float percentage = (Game.ClockTime - _begin) / (_duration / 1000f);
                int width = (int)(_sprite.Width - (percentage * _sprite.Width));
                _countdownText.X = (int) (_sprite.X + (width * _sprite.Scale.X) + GapTextBar);
                _countdownText.text =
                    Math.Round(
                        (Decimal) ((_duration / 1000f) - (Game.ClockTime - _begin)), 1, MidpointRounding.AwayFromZero) +
                    "s";
                Rectangle rect = TextFont.MeasureText(null, _countdownText.text, FontDrawFlags.Center);
                _countdownText.Y = (int) (_sprite.Y - rect.Height / 2 + (_sprite.Height * Program.Instance().BarScale) / 2);
                _sprite.Crop(0, 0, width, _sprite.Height);
            }
            else
            {
                _sprite.Crop(0, 0, _sprite.Width, _sprite.Height);
            }
        }

        public void Reset()
        {
            lastChange = Environment.TickCount;
        }

        public void Scale(float barScale)
        {
            _sprite.Scale = new Vector2(barScale, barScale);
            Reset();
        }
    }
}