using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace UniversalMinimapHack
{
    public class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            MinimapHack.Instance().Load();
            Print("Loaded!");
        }

        public static void Print(string msg)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Universal</font><font color='#d4d4d4'>MinimapHack:</font> <font color='#FFFFFF'>" +
                msg + "</font>");
        }

        public static string Format(float f)
        {
            TimeSpan t = TimeSpan.FromSeconds(f);
            if (t.Minutes < 1)
            {
                return t.Seconds + "";
            }
            if (t.Seconds >= 10)
            {
                return t.Minutes + ":" + t.Seconds;
            }
            return t.Minutes + ":0" + t.Seconds;
        }
    }
}