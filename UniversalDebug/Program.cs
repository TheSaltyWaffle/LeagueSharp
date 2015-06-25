using System;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace UniversalDebug
{
    public class Program
    {
        public static Font font;

        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += delegate
            {
                font = new Font(
                    Drawing.Direct3DDevice, 14, 0, FontWeight.DoNotCare, 0, false, FontCharacterSet.Default,
                    FontPrecision.Default, FontQuality.Antialiased,
                    FontPitchAndFamily.DontCare | FontPitchAndFamily.Decorative, "Tahoma");
                Drawing.OnEndScene += Drawing_OnDraw;
            };
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            //object obj = ObjectManager.Player;
            object obj = ObjectManager.Player;
            int y = 40;
            int x = 20;
            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                if (true)
                {
                    font.DrawText(null, string.Format("{0}: {1}", prop.Name, prop.GetValue(obj, null)), x, y, new ColorBGRA(0, 255, 0, 255));
                    y += 15;
                    if (y > 900)
                    {
                        x += 400;
                        y = 40;
                    }
                }
            }
        }
    }
}