using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace UniversalLeveler
{
    internal static class Program
    {
        private static readonly IDictionary<SpellSlot, int> DefaultSpellSlotPriorities =
            new Dictionary<SpellSlot, int>
            {
                { SpellSlot.Q, 2 },
                { SpellSlot.W, 3 },
                { SpellSlot.E, 4 },
                { SpellSlot.R, 1 }
            };

        private static readonly IList<SpellSlot> SpellSlots = new List<SpellSlot>
        {
            SpellSlot.Q,
            SpellSlot.W,
            SpellSlot.E,
            SpellSlot.R
        };

        private static readonly IDictionary<string, ALevelStrategy> LevelStrategyByChampion =
            new Dictionary<string, ALevelStrategy>
            {
                { "Jayce", new LevelOneUltiStrategy() },
                { "Karma", new LevelOneUltiStrategy() },
                { "Nidalee", new LevelOneUltiStrategy() },
                { "Elise", new LevelOneUltiStrategy() },
                { "Udyr", new UdyrStrategy() },
            };

        private static Menu _menu;
        private static MenuItem _activate;
        private static SpellSlot[] _priority;
        private static IDictionary<MenuItem, int> _menuMap;
        private static bool _lastFormatCorrect = true;
        private static int _level;
        private static ALevelStrategy _levelStrategy;
        private static MenuItem _delay;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void UnitOnOnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (!sender.IsValid || !sender.IsMe || _priority == null ||
                TotalLeveled() - _levelStrategy.LevelOneSkills == 0)
            {
                return;
            }

            foreach (SpellSlot spellSlot in _priority)
            {
                if (ObjectManager.Player.Spellbook.GetSpell(spellSlot).Level == 0 &&
                    args.NewLevel >= GetMinLevel(spellSlot) && args.NewLevel > _levelStrategy.MinimumLevel(spellSlot) &&
                    _levelStrategy.CanLevel(args.NewLevel, spellSlot))
                {
                    Level(spellSlot);
                    return;
                }
            }


            var sl = _activate.GetValue<StringList>();
            if (args.NewLevel >= Int32.Parse(sl.SList[sl.SelectedIndex]))
            {
                SpellSlot spellSlot = _levelStrategy.GetSpellSlotToLevel(args.NewLevel, _priority, false);
                if (spellSlot != SpellSlot.Unknown)
                {
                    Level(spellSlot);
                }
                else
                {
                    SpellSlot spellSlotIgnoreBaseLevel = _levelStrategy.GetSpellSlotToLevel(args.NewLevel, _priority, true);
                    if (spellSlotIgnoreBaseLevel != SpellSlot.Unknown)
                    {
                        Level(spellSlotIgnoreBaseLevel);
                    }
                }
            }
        }

        private static void Level(SpellSlot spellSlot)
        {
            Utility.DelayAction.Add(_delay.GetValue<Slider>().Value, () => ObjectManager.Player.Spellbook.LevelSpell(spellSlot));
        }

        private static int TotalLeveled()
        {
            return SpellSlots.Sum(s => ObjectManager.Player.Spellbook.GetSpell(s).Level);
        }

        private static void OnGameLoad(EventArgs args)
        {
            _menuMap = new Dictionary<MenuItem, int>();
            _menu = new Menu("Universal Leveler", "UniversalLeveler" + ObjectManager.Player.ChampionName, true);
            foreach (var entry in DefaultSpellSlotPriorities)
            {
                MenuItem menuItem = MakeSlider(
                    entry.Key.ToString(), entry.Key.ToString(), entry.Value, 1, DefaultSpellSlotPriorities.Count);
                menuItem.ValueChanged += menuItem_ValueChanged;
                _menu.AddItem(menuItem);

                var subMenu = new Menu(entry.Key + " Extra", entry.Key + "extra");
                subMenu.AddItem(MakeSlider(entry.Key + "extra", "Level after X (inclusive) ?", 1, 1, 18));
                _menu.AddSubMenu(subMenu);
            }

            _activate = new MenuItem("activate", "Start at level?").SetValue(new StringList(new[] { "2", "3", "4" }));
            _delay = new MenuItem("delay", "LevelUp Delay (ms)").SetValue(new Slider(0, 0, 2000));
            _menu.AddItem(_activate);
            _menu.AddItem(_delay);
            _menu.AddToMainMenu();


            foreach (var entry in DefaultSpellSlotPriorities)
            {
                MenuItem item = _menu.GetSlider(entry.Key.ToString());
                _menuMap[item] = item.GetValue<Slider>().Value;
            }

            ParseMenu();
            
            _levelStrategy = new DefaultLevelStrategy();
            if (LevelStrategyByChampion.ContainsKey(ObjectManager.Player.ChampionName))
            {
                _levelStrategy = LevelStrategyByChampion[ObjectManager.Player.ChampionName];
            }
            _level = ObjectManager.Player.Level;

            //CustomEvents.Unit.OnLevelUp += UnitOnOnLevelUp;
            Game.OnUpdate += Game_OnUpdate;
            Print("Loaded!");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            int newLevel = ObjectManager.Player.Level;
            if (_level < newLevel)
            {
                var levelupArgs = new CustomEvents.Unit.OnLevelUpEventArgs
                {
                    NewLevel = newLevel,
                    RemainingPoints = newLevel - _level
                };
                _level = newLevel;

                UnitOnOnLevelUp(ObjectManager.Player, levelupArgs);
            }
        }

        private static void ParseMenu()
        {
            var indices = new bool[DefaultSpellSlotPriorities.Count];
            bool format = true;
            _priority = new SpellSlot[DefaultSpellSlotPriorities.Count];
            foreach (var entry in DefaultSpellSlotPriorities)
            {
                int index = _menuMap[_menu.GetSlider(entry.Key.ToString())] - 1;
                if (indices[index])
                {
                    format = false;
                }

                indices[index] = true;
                _priority[index] = entry.Key;
            }
            if (!format)
            {
                Print("Menu values are <font color='#FF0000'>incorrect</font>!");
                _priority = null;
                _lastFormatCorrect = false;
            }
            else
            {
                if (!_lastFormatCorrect)
                {
                    Print("Menu values are <font color='#008000'>correct</font>!");
                }
                _lastFormatCorrect = true;
            }
        }

        private static void menuItem_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            int oldValue = _menuMap[((MenuItem) sender)];
            int newValue = e.GetNewValue<Slider>().Value;
            if (oldValue != newValue)
            {
                _menuMap[((MenuItem) sender)] = newValue;
                ParseMenu();
            }
        }


        private static void Print(string msg)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Universal</font><font color='#d4d4d4'>Leveler:</font> <font color='#FFFFFF'>" +
                msg + "</font>");
        }

        private static MenuItem MakeSlider(string name, string display, int value, int min, int max)
        {
            var item = new MenuItem(name + ObjectManager.Player.ChampionName, display);
            item.SetValue(new Slider(value, min, max));
            return item;
        }

        private static MenuItem GetSlider(this Menu menu, string name)
        {
            return menu.Item(name + ObjectManager.Player.ChampionName);
        }

        public static int GetMinLevel(SpellSlot s)
        {
            return _menu.SubMenu(s + "extra").GetSlider(s + "extra").GetValue<Slider>().Value;
        }
    }
}