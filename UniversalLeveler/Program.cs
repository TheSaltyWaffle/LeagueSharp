using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace UniversalLeveler
{
    static class Program
    {

        private static readonly IDictionary<SpellSlot, int> SpellShots = new Dictionary<SpellSlot, int>
        {
            {SpellSlot.Q, 2},
            {SpellSlot.W, 3},
            {SpellSlot.E, 4},
            {SpellSlot.R, 1}
        };

        private static Menu _menu;
        private static MenuItem _activate;
        private static SpellSlot[] _priority;
        private static IDictionary<MenuItem, int> _menuMap;
        private static bool _lastFormatCorrect = true;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void UnitOnOnLevelUp(Obj_AI_Base sender, CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (!sender.IsValid || !sender.IsMe || _priority == null || args.NewLevel <= 1)
            {
                return;
            }

            for (int i = 0; i < args.RemainingPoints; i++)
            {
                if (args.NewLevel > 3)
                {
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level == 0 && args.NewLevel >= GetMinLevel(SpellSlot.Q))
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.Q);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level == 0 && args.NewLevel >= GetMinLevel(SpellSlot.W))
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.W);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level == 0 && args.NewLevel >= GetMinLevel(SpellSlot.E))
                    {
                        ObjectManager.Player.Spellbook.LevelUpSpell(SpellSlot.E);
                    }
                }
            }



            StringList sl = _activate.GetValue<StringList>();
            if (args.NewLevel >= Int32.Parse(sl.SList[sl.SelectedIndex]))
            {
                int attempt = 0;
                foreach (SpellSlot s in _priority)
                {
                    for (int i = 0; i < args.RemainingPoints; i++)
                    {
                        attempt++;
                        SpellSlot s1 = s;
                        Utility.DelayAction.Add(Game.Ping * 3 * attempt, delegate
                        {
                            if (((ObjectManager.Player.Spellbook.GetSpell(s1).Level == 0 && args.NewLevel <= 3) ||
                                args.NewLevel > 3) && args.NewLevel >= GetMinLevel(s1))
                            {
                                ObjectManager.Player.Spellbook.LevelUpSpell(s1);
                            }
                        });
                    }
                }
            }

        }

        private static void OnGameLoad(EventArgs args)
        {
            _menuMap = new Dictionary<MenuItem, int>();
            _menu = new Menu("Universal Leveler", "UniversalLeveler" + ObjectManager.Player.ChampionName, true);
            foreach (KeyValuePair<SpellSlot, int> entry in SpellShots)
            {
                MenuItem menuItem = MakeSlider(entry.Key.ToString(), entry.Key.ToString(), entry.Value, 1,
                    SpellShots.Count);
                menuItem.ValueChanged += menuItem_ValueChanged;
                _menu.AddItem(menuItem);

                Menu subMenu = new Menu(entry.Key.ToString() + " Extra", entry.Key.ToString() + "extra");
                subMenu.AddItem(MakeSlider(entry.Key.ToString() + "extra", "Level after (inclusive)?", 1, 1, 18));
                _menu.AddSubMenu(subMenu);
            }

            _activate = new MenuItem("activate", "Level to start?").SetValue(new StringList(new string[] { "2", "3" }));
            _menu.AddItem(_activate);
            _menu.AddToMainMenu();


            foreach (KeyValuePair<SpellSlot, int> entry in SpellShots)
            {
                MenuItem item = _menu.GetSlider(entry.Key.ToString());
                _menuMap[item] = item.GetValue<Slider>().Value;
            }

            ParseMenu();

            CustomEvents.Unit.OnLevelUp += UnitOnOnLevelUp;

            Print("Loaded!");
        }

        private static void ParseMenu()
        {
            bool[] indices = new bool[SpellShots.Count];
            bool format = true;
            _priority = new SpellSlot[SpellShots.Count];
            foreach (KeyValuePair<SpellSlot, int> entry in SpellShots)
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
            int oldValue = _menuMap[((MenuItem)sender)];
            int newValue = e.GetNewValue<Slider>().Value;
            if (oldValue != newValue)
            {
                _menuMap[((MenuItem)sender)] = newValue;
                ParseMenu();
            }
        }


        private static void Print(string msg)
        {
            Game.PrintChat("<font color='#ff3232'>Universal</font><font color='#BABABA'>Leveler:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }

        private static MenuItem MakeSlider(string name, string display, int value, int min, int max)
        {
            MenuItem item = new MenuItem(name + ObjectManager.Player.ChampionName, display);
            item.SetValue(new Slider(value, min, max));
            return item;
        }

        private static MenuItem GetSlider(this Menu menu, string name)
        {
            return menu.Item(name + ObjectManager.Player.ChampionName);
        }

        private static int GetMinLevel(SpellSlot s)
        {
            return _menu.SubMenu(s.ToString() + "extra").GetSlider(s.ToString() + "extra").GetValue<Slider>().Value;
        }
    }
}
