using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace UniversalMinimapHack
{
    internal class Program
    {
        private static Program _instance;
        private string _version;
        private readonly IList<Position> _positions = new List<Position>();
        private MenuItem _slider;
        private MenuItem SsFallbackPing;
        public MenuItem SsTimerEnabler { get; set; }

        private static void Main(string[] args)
        {
            GetInstance();
        }

        public static Program GetInstance()
        {
            if (_instance == null)
            {
                return new Program();
            }
            return _instance;
        }

        private Program()
        {
            _instance = this;
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            Menu menu = new Menu("Universal MinimapHack", "UniversalMinimapHack", true);
            _slider = new MenuItem("scale", "Icon Scale % (F5 to Reload)").SetValue(new Slider(20));
            IconOpacity = new MenuItem("opacity", "Icon Opacity % (F5 to Reload)").SetValue(new Slider(70));
            SsTimerEnabler =
                new MenuItem("enableSS", "Enable SS Timer").SetValue(true);
            SsTimerSize = new MenuItem("sizeSS", "SS Text Size (F5 to Reload)").SetValue(new Slider(15));
            SsTimerOffset = new MenuItem("offsetSS", "SS Text Height").SetValue(new Slider(15, -50, +50));
            SsTimerMin = new MenuItem("minSS", "Show after X seconds").SetValue(new Slider(30, 1, 180));
            SsTimerMinPing = new MenuItem("minPingSS", "Ping after X seconds").SetValue(new Slider(30, 5, 180));
            SsFallbackPing = new MenuItem("fallbackSS", "Fallback ping (local)").SetValue(false);
            menu.AddItem(new MenuItem("", "[Customize]"));
            menu.AddItem(_slider);
            menu.AddItem(IconOpacity);
            Menu ssMenu = new Menu("SS Timer", "ssTimer");
            ssMenu.AddItem(SsTimerEnabler);
            ssMenu.AddItem(new MenuItem("1", "[Extra]"));
            ssMenu.AddItem(SsTimerMin);
            ssMenu.AddItem(SsFallbackPing);
            ssMenu.AddItem(SsTimerMinPing);
            ssMenu.AddItem(new MenuItem("2", "[Customize]"));
            ssMenu.AddItem(SsTimerSize);
            ssMenu.AddItem(SsTimerOffset);
            menu.AddSubMenu(ssMenu);
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
                Game.OnGameUpdate += Game_OnGameUpdate;
                Drawing.OnEndScene += Drawing_OnEndScene;
                Drawing.OnPreReset += Drawing_OnPreReset;
                Drawing.OnPostReset += Drawing_OnPostReset;
            }
            else
            {
                Print("Failed to load ddragon version after " + attempt + 1 + " attempts!");
            }
        }

        private void Drawing_OnPostReset(EventArgs args)
        {
            foreach (Position pos in _positions)
            {
                pos.Text.OnPostReset();
            }
        }

        private void Drawing_OnPreReset(EventArgs args)
        {
            foreach (Position pos in _positions)
            {
                pos.Text.OnPreReset();
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            foreach (Position pos in _positions)
            {
                if (pos.Text.Visible)
                {
                    pos.Text.OnEndScene();
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            foreach (Position pos in _positions)
            {
                if (pos.Hero.ServerPosition != pos.LastLocation)
                {
                    pos.LastLocation = pos.Hero.ServerPosition;
                    pos.LastSeen = Game.ClockTime;
                }

                if (pos.Hero.IsVisible && !pos.Hero.IsDead)
                {
                    pos.Pinged = false;
                }


                if (pos.LastSeen != 0f && SsFallbackPing.GetValue<bool>() && !pos.Hero.IsDead)
                {
                    if (Game.ClockTime - pos.LastSeen >= SsTimerMinPing.GetValue<Slider>().Value && !pos.Pinged)
                    {
                        Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(pos.LastLocation.X, pos.LastLocation.Y, pos.Hero.NetworkId,
                            ObjectManager.Player.NetworkId, Packet.PingType.EnemyMissing)).Process();
                        pos.Pinged = true;
                    } 
                }
                
            }
        }

        private float GetScale()
        {
            return _slider.GetValue<Slider>().Value/100f;
        }

        private void LoadImages()
        {
            foreach (
                Obj_AI_Hero hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero != null && hero.Team != ObjectManager.Player.Team && hero.IsValid))
            {
                LoadImage(hero);
            }
        }

        private void LoadImage(Obj_AI_Hero hero)
        {
            Bitmap bmp = null;
            if (File.Exists(GetImageCached(hero.ChampionName)))
            {
                bmp = new Bitmap(GetImageCached(hero.ChampionName));
            }
            else
            {
                int attempt = 0;
                Bitmap tmp = DownloadImage(hero.ChampionName);
                while (tmp == null && attempt < 5)
                {
                    tmp = DownloadImage(hero.ChampionName);
                    attempt++;
                }

                if (tmp == null)
                {
                    Print("Failed to load " + hero.ChampionName + " after " + attempt + 1 + " attempts!");
                }
                else
                {
                    bmp = CreateFinalImage(tmp, 0, 0, tmp.Width);
                    bmp.Save(GetImageCached(hero.ChampionName));
                    tmp.Dispose();
                }
            }

            if (bmp != null)
            {
                Position pos = new Position(hero, ChangeOpacity(bmp, IconOpacity.GetValue<Slider>().Value/100f),
                    GetScale());
                _positions.Add(pos);
            }
        }

        private Bitmap DownloadImage(string champName)
        {
            WebRequest request =
                WebRequest.Create("http://ddragon.leagueoflegends.com/cdn/" + _version + "/img/champion/" + champName +
                                  ".png");
            Stream responseStream;
            using (WebResponse response = request.GetResponse())
            using (responseStream = response.GetResponseStream())
                return responseStream != null ? new Bitmap(responseStream) : null;
        }

        public Bitmap CreateFinalImage(Bitmap srcBitmap, int circleUpperLeftX, int circleUpperLeftY, int circleDiameter)
        {
            Bitmap finalImage = new Bitmap(circleDiameter, circleDiameter);
            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(circleUpperLeftX, circleUpperLeftY,
                circleDiameter, circleDiameter);

            using (Bitmap sourceImage = srcBitmap)
            using (Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat))
            using (TextureBrush tb = new TextureBrush(croppedImage))
            using (Graphics g = Graphics.FromImage(finalImage))
            {
                g.FillEllipse(tb, 0, 0, circleDiameter, circleDiameter);
                Pen p = new Pen(System.Drawing.Color.DarkRed, 10) {Alignment = PenAlignment.Inset};
                g.DrawEllipse(p, 0, 0, circleDiameter, circleDiameter);
            }
            return finalImage;
        }

        public static Bitmap ChangeOpacity(Bitmap img, float opacityvalue)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
            Graphics graphics = Graphics.FromImage(bmp);
            ColorMatrix colormatrix = new ColorMatrix {Matrix33 = opacityvalue};
            ImageAttributes imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(img, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width,
                img.Height, GraphicsUnit.Pixel, imgAttribute);
            graphics.Dispose(); // Releasing all resource used by graphics
            img.Dispose();
            return bmp;
        }

        private void Print(string msg)
        {
            Game.PrintChat(
                "<font color='#ff3232'>Universal</font><font color='#BABABA'>MinimapHack:</font> <font color='#FFFFFF'>" +
                msg + "</font>");
        }

        public string GameVersion()
        {
            String json = new WebClient().DownloadString("http://ddragon.leagueoflegends.com/realms/euw.json");
            return (string) new JavaScriptSerializer().Deserialize<Dictionary<String, Object>>(json)["v"];
        }

        public string GetImageCached(string champName)
        {
            string path = Path.GetTempPath() + "UniversalMinimapHack";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += "\\" + _version;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path + "\\" + champName + ".png";
        }


        public MenuItem SsTimerSize { get; set; }

        public MenuItem SsTimerOffset { get; set; }

        public MenuItem IconOpacity { get; set; }

        public MenuItem SsTimerMin { get; set; }

        public MenuItem SsTimerMinPing { get; set; }
    }


    public class Position
    {
        private static int _layer;

        public Render.Sprite Image { get; set; }
        public Render.Text Text { get; set; }
        public Obj_AI_Hero Hero { get; set; }
        public float LastSeen { get; set; }
        public Vector3 LastLocation { get; set; }
        public Vector3 LastLocationVisible { get; set; }
        public bool Pinged { get; set; }

        public Position(Obj_AI_Hero hero, Bitmap bmp, float scale)
        {
            Hero = hero;
            Image = new Render.Sprite(bmp, new Vector2(0, 0));
            Image.GrayScale();
            Image.Scale = new Vector2(scale, scale);
            Image.VisibleCondition = sender => !hero.IsVisible;
            Image.PositionUpdate = delegate
            {
                Vector2 v2 = Drawing.WorldToMinimap(hero.ServerPosition);
                v2.X -= Image.Width/2f;
                v2.Y -= Image.Height/2f;
                return v2;
            };
            Image.Add(_layer);
            LastSeen = 0;
            LastLocation = hero.ServerPosition;
            LastLocationVisible = hero.ServerPosition;

            Text = new Render.Text(0, 0, "", Program.GetInstance().SsTimerSize.GetValue<Slider>().Value,
                SharpDX.Color.White)
            {
                VisibleCondition =
                    sender =>
                        !hero.IsVisible && Program.GetInstance().SsTimerEnabler.GetValue<bool>() && LastSeen > 20f &&
                        Program.GetInstance().SsTimerMin.GetValue<Slider>().Value <= Game.ClockTime - LastSeen,
                PositionUpdate = delegate
                {
                    Vector2 v2 = Drawing.WorldToMinimap(hero.ServerPosition);
                    v2.Y += Program.GetInstance().SsTimerOffset.GetValue<Slider>().Value;
                    return v2;
                },
                TextUpdate = () => Format(Game.ClockTime - LastSeen),
                OutLined = true,
                Centered = true
            };
            Text.Add(_layer);
            _layer++;
        }

        private string Format(float f)
        {
            TimeSpan t = TimeSpan.FromSeconds(f);
            if (t.Minutes < 1) return t.Seconds + "";
            if (t.Seconds >= 10)
            {
                return t.Minutes + ":" + t.Seconds;
            }
            return t.Minutes + ":0" + t.Seconds;
        }
    }
}