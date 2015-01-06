using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UniversalGankAlerter
{
    internal class Program
    {
        private static Program _instance;

        private readonly IDictionary<int, ChampionInfo> _championInfoById = new Dictionary<int, ChampionInfo>();
        private Menu _menu;
        private MenuItem _sliderRadius;
        private PreviewCircle _previewCircle;
        private MenuItem _sliderCooldown;
        private MenuItem _sliderLineDuration;
        private MenuItem _dangerPing;
        private MenuItem _junglerOnly;

        public int Radius
        {
            get { return _sliderRadius.GetValue<Slider>().Value; }
        }

        public int Cooldown
        {
            get { return _sliderCooldown.GetValue<Slider>().Value; }
        }

        public int LineDuration
        {
            get { return _sliderLineDuration.GetValue<Slider>().Value; }
        }

        public bool DangerPing
        {
            get { return _dangerPing.GetValue<bool>(); }
        }

        public bool JunglerOnly
        {
            get { return _junglerOnly.GetValue<bool>(); }
        }

        private static void Main(string[] args)
        {
            _instance = new Program();
        }

        public static Program Instance()
        {
            return _instance;
        }

        private Program()
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            _previewCircle = new PreviewCircle();

            _menu = new Menu("Universal GankAlerter", "universalgankalerter", true);
            _sliderRadius = new MenuItem("range", "Max Range").SetValue(new Slider(3000, 500, 5000));
            _sliderRadius.ValueChanged += SliderRadiusValueChanged;
            _sliderCooldown = new MenuItem("cooldown", "Cooldown (seconds)").SetValue(new Slider(10, 0, 60));
            _sliderLineDuration = new MenuItem("lineduration", "Line Duration (seconds)").SetValue(new Slider(10, 0, 20));
            _dangerPing = new MenuItem("dangerping", "Danger Ping (local)").SetValue(true);
            _junglerOnly = new MenuItem("jungleronly", "Warn Jungler Only").SetValue(false);


            _menu.AddItem(_sliderRadius);
            _menu.AddItem(_sliderCooldown);
            _menu.AddItem(_sliderLineDuration);
            _menu.AddItem(_dangerPing);
            _menu.AddItem(_junglerOnly);
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.Team != ObjectManager.Player.Team)
                {
                    _championInfoById[hero.NetworkId] = new ChampionInfo(hero);
                    _menu.AddItem(new MenuItem(hero.ChampionName, hero.ChampionName).SetValue(true));
                }
            }
            _menu.AddToMainMenu();
            Print("Loaded!");
        }

        private void SliderRadiusValueChanged(object sender, OnValueChangeEventArgs e)
        {
            _previewCircle.SetRadius(e.GetNewValue<Slider>().Value);
        }

        private static void Print(string msg)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Universal</font><font color='#d4d4d4'>GankAlerter:</font> <font color='#FFFFFF'>" +
                msg + "</font>");
        }

        public bool IsEnabled(Obj_AI_Hero hero)
        {
            return _menu.Item(hero.ChampionName).GetValue<bool>();
        }
    }

    internal class PreviewCircle
    {
        private const int Delay = 2;

        private float _lastChanged;
        private readonly Render.Circle _mapCircle;
        private int _radius;

        public PreviewCircle()
        {
            Drawing.OnEndScene += Drawing_OnEndScene;
            _mapCircle = new Render.Circle(ObjectManager.Player, 0, System.Drawing.Color.Red, 5);
            _mapCircle.Add(0);
            _mapCircle.VisibleCondition = sender => _lastChanged > 0 && Game.ClockTime - _lastChanged < Delay;
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (_lastChanged > 0 && Game.ClockTime - _lastChanged < Delay)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, _radius, System.Drawing.Color.Red, 2, 30, true);
            }
        }

        public void SetRadius(int radius)
        {
            _radius = radius;
            _mapCircle.Radius = radius;
            _lastChanged = Game.ClockTime;
        }
    }

    internal class ChampionInfo
    {
        private readonly Obj_AI_Hero _hero;

        private event EventHandler OnEnterRange;

        private bool _visible;
        private float _distance;
        private float _lastEnter;
        private float _lineStart;

        public ChampionInfo(Obj_AI_Hero hero)
        {
            _hero = hero;
            Render.Line line = new Render.Line(new Vector2(), new Vector2(), 5,
                new Color {R = 255, G = 0, B = 0, A = 125})
            {
                StartPositionUpdate = () => Drawing.WorldToScreen(ObjectManager.Player.Position),
                EndPositionUpdate = () => Drawing.WorldToScreen(_hero.Position),
                VisibleCondition =
                    delegate
                    {
                        return !_hero.IsDead &&
                               Game.ClockTime - _lineStart < Program.Instance().LineDuration;
                    }
            };
            line.Add(0);
            Game.OnGameUpdate += Game_OnGameUpdate;
            OnEnterRange += ChampionInfo_OnEnterRange;
        }

        private void ChampionInfo_OnEnterRange(object sender, EventArgs e)
        {
            if (Game.ClockTime - _lastEnter > Program.Instance().Cooldown &&
                ((IsJungler(_hero) && Program.Instance().JunglerOnly) || !Program.Instance().JunglerOnly) && Program.Instance().IsEnabled(_hero))
            {
                _lineStart = Game.ClockTime;
                if (Program.Instance().DangerPing)
                {
                    Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(_hero.Position.X, _hero.Position.Y,
                        0,0, Packet.PingType.Danger)).Process();
                }
            }
            _lastEnter = Game.ClockTime;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            float newDistance = _hero.Distance(ObjectManager.Player);
            if (newDistance < Program.Instance().Radius && _hero.IsVisible)
            {
                if (_distance >= Program.Instance().Radius || !_visible)
                {
                    if (OnEnterRange != null)
                    {
                        OnEnterRange(this, null);
                    }
                }
                else if (_distance < Program.Instance().Radius && _visible)
                {
                    _lastEnter = Game.ClockTime;
                }
            }
            _distance = newDistance;
            _visible = _hero.IsVisible;
        }

        private bool IsJungler(Obj_AI_Hero hero)
        {
            return hero.Spellbook.Spells.Any(spell => spell.Name.ToLower().Contains("smite"));
        }
    }
}