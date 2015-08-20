using System.Drawing;
using LeagueSharp.Common;

namespace UniversalMinimapHack
{
    public class Menu : LeagueSharp.Common.Menu
    {
        private readonly MenuItem _iconOpacity;
        private readonly MenuItem _slider;
        private readonly MenuItem _ssCircle;
        private readonly MenuItem _ssCircleColor;
        private readonly MenuItem _ssCircleSize;
        private readonly MenuItem _ssFallbackPing;
        private readonly MenuItem _ssTimerEnabler;
        private readonly MenuItem _ssTimerMin;
        private readonly MenuItem _ssTimerMinPing;
        private readonly MenuItem _ssTimerOffset;
        private readonly MenuItem _ssTimerSize;

        public Menu() : base("Universal MinimapHack", "UniversalMinimapHack", true)
        {
            _slider = new MenuItem("scale", "Icon Scale % (F5 to Reload)").SetValue(new Slider(20));
            _iconOpacity = new MenuItem("opacity", "Icon Opacity % (F5 to Reload)").SetValue(new Slider(70));
            _ssTimerEnabler = new MenuItem("enableSS", "Enable").SetValue(true);
            _ssTimerSize = new MenuItem("sizeSS", "SS Text Size (F5 to Reload)").SetValue(new Slider(15));
            _ssTimerOffset = new MenuItem("offsetSS", "SS Text Height").SetValue(new Slider(15, -50, +50));
            _ssTimerMin = new MenuItem("minSS", "Show after X seconds").SetValue(new Slider(30, 1, 180));
            _ssTimerMinPing = new MenuItem("minPingSS", "Ping after X seconds").SetValue(new Slider(30, 5, 180));
            _ssFallbackPing = new MenuItem("fallbackSS", "Fallback ping (local)").SetValue(false);
            AddItem(new MenuItem("", "[Customize]"));
            AddItem(_slider);
            AddItem(_iconOpacity);
            var ssMenu = new LeagueSharp.Common.Menu("SS Timer", "ssTimer");
            ssMenu.AddItem(_ssTimerEnabler);
            ssMenu.AddItem(new MenuItem("1", "--- [Extra] ---"));
            ssMenu.AddItem(_ssTimerMin);
            ssMenu.AddItem(_ssFallbackPing);
            ssMenu.AddItem(_ssTimerMinPing);
            ssMenu.AddItem(new MenuItem("2", "--- [Customize] ---"));
            ssMenu.AddItem(_ssTimerSize);
            ssMenu.AddItem(_ssTimerOffset);
            var ssCircleMenu = new LeagueSharp.Common.Menu("SS Circles", "ccCircles");
            _ssCircle = new MenuItem("ssCircle", "Enable").SetValue(true);
            _ssCircleSize = new MenuItem("ssCircleSize", "Max Circle Size").SetValue(new Slider(7000, 500, 15000));
            _ssCircleColor = new MenuItem("ssCircleColor", "Circle color").SetValue(System.Drawing.Color.Green);
            ssCircleMenu.AddItem(_ssCircle);
            ssCircleMenu.AddItem(_ssCircleSize);
            ssCircleMenu.AddItem(_ssCircleColor);
            AddSubMenu(ssMenu);
            AddSubMenu(ssCircleMenu);
            AddToMainMenu();
        }

        public float IconScale
        {
            get { return _slider.GetValue<Slider>().Value / 100f; }
        }

        public float IconOpacity
        {
            get { return _iconOpacity.GetValue<Slider>().Value / 100f; }
        }

        // ReSharper disable once InconsistentNaming
        public bool SSTimer
        {
            get { return _ssTimerEnabler.GetValue<bool>(); }
        }

        // ReSharper disable once InconsistentNaming
        public int SSTimerSize
        {
            get { return _ssTimerSize.GetValue<Slider>().Value; }
        }

        // ReSharper disable once InconsistentNaming
        public int SSTimerOffset
        {
            get { return _ssTimerOffset.GetValue<Slider>().Value; }
        }

        // ReSharper disable once InconsistentNaming
        public int SSTimerStart
        {
            get { return _ssTimerMin.GetValue<Slider>().Value; }
        }

        // ReSharper disable once InconsistentNaming
        public bool Ping
        {
            get { return _ssFallbackPing.GetValue<bool>(); }
        }

        // ReSharper disable once InconsistentNaming
        public int MinPing
        {
            get { return _ssTimerMinPing.GetValue<Slider>().Value; }
        }

        // ReSharper disable once InconsistentNaming
        public bool SSCircle
        {
            get { return _ssCircle.GetValue<bool>(); }
        }

        // ReSharper disable once InconsistentNaming
        public int SSCircleSize
        {
            get { return _ssCircleSize.GetValue<Slider>().Value; }
        }

        // ReSharper disable once InconsistentNaming
        public Color SSCircleColor
        {
            get { return _ssCircleColor.GetValue<Color>(); }
        }
    }
}