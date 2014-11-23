using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using System.Web.Script.Serialization;

namespace UniversalMapHack
{
    class Program
    {
        private static string _version;
        private static readonly IList<Render.Sprite> Sprites = new List<Render.Sprite>();
        private static MenuItem _slider;
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Menu menu = new Menu("Universal MapHack", "UniversalMapHack", true);
            _slider = new MenuItem("scale", "Scale % (F5 to Reload)").SetValue(new Slider(20));
            menu.AddItem(_slider);
            menu.AddToMainMenu();

            int attempt = 0;
            _version = GameVersion();
            while (string.IsNullOrEmpty(_version) && attempt < 5)
            {
                _version = GameVersion();
                attempt++;
            }


            if (!string.IsNullOrEmpty(_version))
            {
                LoadImages();
                Print("Loaded!");
            }
            else
            {
                Print("Failed to load ddragon version after " + attempt + 1 + " attempts!");
            }
        }

        static float GetScale()
        {
            return _slider.GetValue<Slider>().Value / 100f;
        }

        static void LoadImages()
        {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero != null && hero.Team != ObjectManager.Player.Team && hero.IsValid))
            {
                LoadImage(hero);
            }
        }

        static void LoadImage(Obj_AI_Hero hero)
        {
            int attempt = 0;
            Bitmap tmp = DownloadImage(hero.ChampionName);
            while (tmp == null && attempt < 5)
            {
                tmp = DownloadImage(hero.ChampionName);
                attempt++;
            }
            if (tmp != null)
            {
                Bitmap tmp2 = CreateFinalImage(tmp, 0, 0, tmp.Width);
                tmp.Dispose();
                Render.Sprite sprite = new Render.Sprite(tmp2, new Vector2(0, 0));
                sprite.GrayScale();
                sprite.Scale = new Vector2(GetScale(), GetScale());
                Obj_AI_Hero hero1 = hero;
                sprite.VisibleCondition = sender => !hero1.IsVisible;
                sprite.PositionUpdate = delegate
                {
                    Vector2 v2 = Drawing.WorldToMinimap(hero1.ServerPosition);
                    v2.X -= sprite.Width / 2f;
                    v2.Y -= sprite.Height / 2f;
                    return v2;
                };
                sprite.Add(0);
                Sprites.Add(sprite);
            }
            else
            {
                Print("Failed to load " + hero.ChampionName + " after " + attempt + 1 + " attempts!");
            }
        }

        private static Bitmap DownloadImage(string champName)
        {
            WebRequest request = WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + _version + "/img/champion/" + champName + ".png");
            System.IO.Stream responseStream;
            using (WebResponse response = request.GetResponse())
            using (responseStream = response.GetResponseStream())
                return responseStream != null ? new Bitmap(responseStream) : null;
        }

        public static Bitmap CreateFinalImage(Bitmap srcBitmap, int circleUpperLeftX, int circleUpperLeftY, int circleDiameter)
        {
            Bitmap finalImage = new Bitmap(circleDiameter, circleDiameter);
            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(circleUpperLeftX, circleUpperLeftY, circleDiameter, circleDiameter);

            using (Bitmap sourceImage = srcBitmap)
            using (Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat))
            using (TextureBrush tb = new TextureBrush(croppedImage))
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.FillEllipse(tb, 0, 0, circleDiameter, circleDiameter);
                Pen p = new Pen(System.Drawing.Color.DarkRed, 10) { Alignment = PenAlignment.Inset };
                g.DrawEllipse(p, 0, 0, circleDiameter, circleDiameter);
            }

            return finalImage;
        }

        private static void Print(string msg)
        {
            Game.PrintChat("<font color='#ff3232'>Universal</font><font color='#BABABA'>MapHack:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }

        public static string GameVersion()
        {
            String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
            return (string)new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
        }

    }
}
