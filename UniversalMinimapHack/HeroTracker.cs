using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = SharpDX.Color;

namespace UniversalMinimapHack
{
    public class HeroTracker
    {
        public HeroTracker(Obj_AI_Hero hero, Bitmap bmp)
        {
            Hero = hero;

            RecallStatus = Packet.S2C.Teleport.Status.Unknown;
            Hero = hero;
            var image = new Render.Sprite(bmp, new Vector2(0, 0));
            image.GrayScale();
            image.Scale = new Vector2(MinimapHack.Instance().Menu.IconScale, MinimapHack.Instance().Menu.IconScale);
            image.VisibleCondition = sender => !hero.IsVisible && !hero.IsDead;
            image.PositionUpdate = delegate
            {
                Vector2 v2 = Drawing.WorldToMinimap(LastLocation);
                v2.X -= image.Width / 2f;
                v2.Y -= image.Height / 2f;
                return v2;
            };
            image.Add(0);
            LastSeen = 0;
            LastLocation = hero.ServerPosition;
            PredictedLocation = hero.ServerPosition;
            BeforeRecallLocation = hero.ServerPosition;

            Text = new Render.Text(0, 0, "", MinimapHack.Instance().Menu.SSTimerSize, Color.White)
            {
                VisibleCondition =
                    sender =>
                        !hero.IsVisible && !Hero.IsDead && MinimapHack.Instance().Menu.SSTimer && LastSeen > 20f &&
                        MinimapHack.Instance().Menu.SSTimerStart <= Game.ClockTime - LastSeen,
                PositionUpdate = delegate
                {
                    Vector2 v2 = Drawing.WorldToMinimap(LastLocation);
                    v2.Y += MinimapHack.Instance().Menu.SSTimerOffset;
                    return v2;
                },
                TextUpdate = () => Program.Format(Game.ClockTime - LastSeen),
                OutLined = true,
                Centered = true
            };
            Text.Add(0);

            Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnEndScene += Drawing_OnEndScene;
        }

        private Render.Text Text { get; set; }
        private Obj_AI_Hero Hero { get; set; }
        private Packet.S2C.Teleport.Status RecallStatus { get; set; }
        private float LastSeen { get; set; }
        private Vector3 LastLocation { get; set; }
        private Vector3 PredictedLocation { get; set; }
        private Vector3 BeforeRecallLocation { get; set; }
        private bool Pinged { get; set; }

        private void Drawing_OnEndScene(EventArgs args)
        {
            if (!Hero.IsVisible && !Hero.IsDead)
            {
                float radius = Math.Abs(LastLocation.X - PredictedLocation.X);
                if (radius < MinimapHack.Instance().Menu.SSCircleSize && MinimapHack.Instance().Menu.SSCircle)
                {
                    System.Drawing.Color c = MinimapHack.Instance().Menu.SSCircleColor;
                    if (RecallStatus == Packet.S2C.Teleport.Status.Start)
                    {
                        c = System.Drawing.Color.LightBlue;
                    }
                    
                    Utility.DrawCircle(LastLocation, radius, c, 1, 30, true);
                }
            }
            if (Text.Visible)
            {
                Text.OnEndScene();
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Hero.ServerPosition != LastLocation && Hero.ServerPosition != BeforeRecallLocation)
            {
                LastLocation = Hero.ServerPosition;
                PredictedLocation = Hero.ServerPosition;
                LastSeen = Game.ClockTime;
            }

            if (!Hero.IsVisible && RecallStatus != Packet.S2C.Teleport.Status.Start)
            {
                PredictedLocation = new Vector3(
                    LastLocation.X + ((Game.ClockTime - LastSeen) * Hero.MoveSpeed), LastLocation.Y, LastLocation.Z);
            }

            if (Hero.IsVisible && !Hero.IsDead)
            {
                Pinged = false;
                LastSeen = Game.ClockTime;
            }

            if (LastSeen > 0f && MinimapHack.Instance().Menu.Ping && !Hero.IsVisible)
            {
                if (Game.ClockTime - LastSeen >= MinimapHack.Instance().Menu.MinPing && !Pinged)
                {
                    Game.ShowPing(PingCategory.EnemyMissing,Hero,true);
                    Pinged = true;
                }
            }
        }

        private void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
            if (decoded.UnitNetworkId == Hero.NetworkId && decoded.Type == Packet.S2C.Teleport.Type.Recall)
            {
                RecallStatus = decoded.Status;
                if (decoded.Status == Packet.S2C.Teleport.Status.Finish)
                {
                    BeforeRecallLocation = Hero.ServerPosition;
                    Obj_SpawnPoint enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
                    if (enemySpawn != null)
                    {
                        LastLocation = enemySpawn.Position;
                        PredictedLocation = enemySpawn.Position;
                    }
                    LastSeen = Game.ClockTime;
                }
            }
        }
    }
}