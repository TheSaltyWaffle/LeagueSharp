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
        private MenuItem _enemyJunglerOnly;
        private MenuItem _allyJunglerOnly;
        private MenuItem _showChampionNames;
        private MenuItem _drawMinimapLines;
        private MenuItem _dangerPing;
        private Menu _enemies;
        private Menu _allies;

        public int Radius
        {
            get { return _sliderRadius.GetValue<Slider>().Value; }
        }

        public int Cooldown
        {
            get { return _sliderCooldown.GetValue<Slider>().Value; }
        }

        public bool DangerPing
        {
            get { return _dangerPing.GetValue<bool>(); }
        }

        public int LineDuration
        {
            get { return _sliderLineDuration.GetValue<Slider>().Value; }
        }

        public bool EnemyJunglerOnly
        {
            get { return _enemyJunglerOnly.GetValue<bool>(); }
        }

        public bool AllyJunglerOnly
        {
            get { return _allyJunglerOnly.GetValue<bool>(); }
        }

        public bool ShowChampionNames
        {
            get { return _showChampionNames.GetValue<bool>(); }
        }

        public bool DrawMinimapLines
        {
            get { return _drawMinimapLines.GetValue<bool>(); }
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
            _sliderRadius = new MenuItem("range", "Trigger range").SetValue(new Slider(3000, 500, 5000));
            _sliderRadius.ValueChanged += SliderRadiusValueChanged;
            _sliderCooldown = new MenuItem("cooldown", "Trigger cooldown (sec)").SetValue(new Slider(10, 0, 60));
            _sliderLineDuration = new MenuItem("lineduration", "Line duration (sec)").SetValue(new Slider(10, 0, 20));
            _enemyJunglerOnly = new MenuItem("jungleronly", "Warn jungler only (smite)").SetValue(false);
            _allyJunglerOnly = new MenuItem("allyjungleronly", "Warn jungler only (smite)").SetValue(true);
            _showChampionNames = new MenuItem("shownames", "Show champion name").SetValue(true);
            _drawMinimapLines = new MenuItem("drawminimaplines", "Draw minimap lines").SetValue(false);
            _dangerPing = new MenuItem("dangerping", "Danger Ping (local)").SetValue(false);
            _enemies = new Menu("Enemies", "enemies");
            _enemies.AddItem(_enemyJunglerOnly);

            _allies = new Menu("Allies", "allies");
            _allies.AddItem(_allyJunglerOnly);

            _menu.AddItem(_sliderRadius);
            _menu.AddItem(_sliderCooldown);
            _menu.AddItem(_sliderLineDuration);
            _menu.AddItem(_showChampionNames);
            _menu.AddItem(_drawMinimapLines);
            _menu.AddItem(_dangerPing);
            _menu.AddSubMenu(_enemies);
            _menu.AddSubMenu(_allies);
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (hero.NetworkId != ObjectManager.Player.NetworkId)
                {
                    if (hero.IsEnemy)
                    {
                        _championInfoById[hero.NetworkId] = new ChampionInfo(hero, false);
                        _enemies.AddItem(new MenuItem("enemy" + hero.ChampionName, hero.ChampionName).SetValue(true));
                    }
                    else
                    {
                        _championInfoById[hero.NetworkId] = new ChampionInfo(hero, true);
                        _allies.AddItem(new MenuItem("ally" + hero.ChampionName, hero.ChampionName).SetValue(false));
                    }
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
            return hero.IsEnemy
                ? _enemies.Item("enemy" + hero.ChampionName).GetValue<bool>()
                : _allies.Item("ally" + hero.ChampionName).GetValue<bool>();
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
        private static int index = 0;

        private readonly Obj_AI_Hero _hero;

        private event EventHandler OnEnterRange;

        private bool _visible;
        private float _distance;
        private float _lastEnter;
        private float _lineStart;
        private readonly Render.Line _line;

        public ChampionInfo(Obj_AI_Hero hero, bool ally)
        {
            index++;
            int textoffset = index * 50;
            _hero = hero;
            Render.Text text = new Render.Text(
                new Vector2(), _hero.ChampionName, 20,
                ally
                    ? new Color { R = 205, G = 255, B = 205, A = 255 }
                    : new Color { R = 255, G = 205, B = 205, A = 255 })
            {
                PositionUpdate =
                    () =>
                        Drawing.WorldToScreen(
                            ObjectManager.Player.Position.Extend(_hero.Position, 300 + textoffset)),
                VisibleCondition = delegate
                {
                    float dist = _hero.Distance(ObjectManager.Player.Position);
                    return Program.Instance().ShowChampionNames && !_hero.IsDead &&
                           Game.ClockTime - _lineStart < Program.Instance().LineDuration &&
                           (!_hero.IsVisible || !Render.OnScreen(Drawing.WorldToScreen(_hero.Position))) &&
                           dist < Program.Instance().Radius && dist > 300 + textoffset;
                },
                Centered = true,
                OutLined = true,
            };
            text.Add(1);
            _line = new Render.Line(
                new Vector2(), new Vector2(), 5,
                ally ? new Color { R = 0, G = 255, B = 0, A = 125 } : new Color { R = 255, G = 0, B = 0, A = 125 })
            {
                StartPositionUpdate = () => Drawing.WorldToScreen(ObjectManager.Player.Position),
                EndPositionUpdate = () => Drawing.WorldToScreen(_hero.Position),
                VisibleCondition =
                    delegate
                    {
                        return !_hero.IsDead && Game.ClockTime - _lineStart < Program.Instance().LineDuration &&
                               _hero.Distance(ObjectManager.Player.Position) < (Program.Instance().Radius + 1000);
                    }
            };
            _line.Add(0);
            Render.Line minimapLine = new Render.Line(
                new Vector2(), new Vector2(), 2,
                ally ? new Color { R = 0, G = 255, B = 0, A = 255 } : new Color { R = 255, G = 0, B = 0, A = 255 })
            {
                StartPositionUpdate = () => Drawing.WorldToMinimap(ObjectManager.Player.Position),
                EndPositionUpdate = () => Drawing.WorldToMinimap(_hero.Position),
                VisibleCondition =
                    delegate
                    {
                        return Program.Instance().DrawMinimapLines && !_hero.IsDead && Game.ClockTime - _lineStart < Program.Instance().LineDuration;
                    }
            };
            minimapLine.Add(0);
            Game.OnUpdate += Game_OnGameUpdate;
            OnEnterRange += ChampionInfo_OnEnterRange;
        }

        private void ChampionInfo_OnEnterRange(object sender, EventArgs e)
        {
            bool enabled = false;
            if (Program.Instance().EnemyJunglerOnly && _hero.IsEnemy)
            {
                enabled = IsJungler(_hero);
            }
            else if (Program.Instance().AllyJunglerOnly && _hero.IsAlly)
            {
                enabled = IsJungler(_hero);
            }
            else
            {
                enabled = Program.Instance().IsEnabled(_hero);
            }

            if (Game.ClockTime - _lastEnter > Program.Instance().Cooldown && enabled)
            {
                _lineStart = Game.ClockTime;
                if (Program.Instance().DangerPing && _hero.IsEnemy && !_hero.IsDead)
                {
                    Game.ShowPing(PingCategory.Danger,_hero, true);
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

            if (Game.ClockTime - _lineStart < Program.Instance().LineDuration)
            {
                float percentage = newDistance / Program.Instance().Radius;
                if (percentage <= 1)
                {
                    _line.Width = (int) (2 + (percentage * 8));
                }
            }

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