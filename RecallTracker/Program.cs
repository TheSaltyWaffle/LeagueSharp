using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
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
            GameObject.OnCreate += GameObject_OnCreate;
            //Properties.Resources.RecallBar.Save("test123.png");
            float x = (Drawing.Direct3DDevice.Viewport.Width - Properties.Resources.RecallBar.Width)/2f;
            float y = Drawing.Direct3DDevice.Viewport.Height*3f/4f;
            _sprite = new Render.Sprite(Properties.Resources.RecallBar, new Vector2(x,y));
            _sprite.Crop(0, 0, 20, Properties.Resources.RecallBar.Height);
            _sprite.Add(0);
            Game.OnGameProcessPacket += Game_OnGameProcessPacket;
            Game.PrintChat("Test 2");
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name.StartsWith("Minion") && sender.Team == ObjectManager.Player.Team)
            {
                Obj_AI_Minion minion = (Obj_AI_Minion) sender;
                Game.PrintChat(sender.Name);
                //GameObjectType.
            }
            
        }
    }
}
