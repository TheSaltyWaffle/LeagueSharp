using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RecallTracker
{
    public class Program
    {
        private static Render.Sprite _sprite;
        public static void Main(string[] args)
        {
            Game.OnGameUpdate += GameOnOnGameUpdate;
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Drawing.OnDraw += DrawingOnOnDraw;
        }

        private static void GameOnOnGameUpdate(EventArgs args)
        {
            throw new NotImplementedException();
        }

        private static void DrawingOnOnDraw(EventArgs args)
        {
            //
        }

        public static void Game_OnGameProcessPacket(GamePacketEventArgs args)
        {
            if (args.PacketData[0] == Packet.S2C.Recall.Header)
            {
                Packet.S2C.Recall.Struct decoded = Packet.S2C.Recall.Decoded(args.PacketData);
                Obj_AI_Hero objAiHero = ObjectManager.GetUnitByNetworkId<Obj_AI_Hero>(decoded.UnitNetworkId);
                Packet.S2C.Recall.RecallStatus recallStatus = decoded.Status;
                Game.PrintChat(objAiHero.Name + " is " + recallStatus);
            }
        }

        public static void Game_OnGameLoad(EventArgs args)
        {
            Game.PrintChat(Drawing.Direct3DDevice.Viewport.Height + "");
            float x = (Drawing.Direct3DDevice.Viewport.Width - Properties.Resources.RecallBar.Width)/2f;
            float y = Drawing.Direct3DDevice.Viewport.Height*3f/4f;
            _sprite = new Render.Sprite(Properties.Resources.RecallBar, new Vector2(x,y));
            _sprite.Crop(0, 0, 20, Properties.Resources.RecallBar.Height);
            _sprite.Add(0);
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Game.PrintChat("Test 2");
        }
    }
}
