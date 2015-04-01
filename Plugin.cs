using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Linq;
using Newtonsoft.Json;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using System.Text.RegularExpressions;

namespace TentCamping
{
    [ApiVersion(1, 17)]
    public class Plugin : TerrariaPlugin
    {
        public override string Name { get { return "TentCamping"; } }
        public override string Author { get { return "Patrikk"; } }
        public override string Description { get { return "A plugin to set a tent and sleep in it!"; } }
        public override Version Version { get { return new Version(1, 0); } }

        private static List<int> breakables = new List<int>() { 3, 24, 32, 51, 52, 61, 69, 73, 74, 82, 83, 84, 110, 113, 115, 205 };
        public static Dictionary<int, DataStorage> storage = new Dictionary<int, DataStorage>();
        public NetItem[] inventory = new NetItem[NetItem.maxNetInventory];
        public Plugin(Main game)
            : base(game)
        {
        }

        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

        }
        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);



            }
            base.Dispose(Disposing);
        }
        #endregion

        private void OnInitialize(EventArgs args)
        {

            Commands.ChatCommands.Add(new Command("tentcamping.set", Tent, "tent") { AllowServer = false, HelpText = "Allows user to set a tent and sleep in it!" });
        }

        public static void OnLeave(LeaveEventArgs args)
        {
            if (storage.ContainsKey(args.Who))
            {
                WorldGen.KillTile((int)storage[args.Who].tentpos.X, (int)storage[args.Who].tentpos.Y);
                TSPlayer.All.SendTileSquare((int)storage[args.Who].tentpos.X, (int)storage[args.Who].tentpos.Y);
                storage.Remove(args.Who);
            }

        }


        private static DateTime lastUpdate = DateTime.Now;
        public static void OnUpdate(EventArgs args)
        {
            /* try
             {*/
            if ((DateTime.Now - lastUpdate).TotalMilliseconds >= 20)
            {
                lastUpdate = DateTime.Now;
                DistanceCheck();
            }


            // }
            //catch (Exception ex) { Log.ConsoleError(ex.ToString()); }
        }


        private static void KillTent(int index)
        {
                WorldGen.KillTile((int)storage[index].tentpos.X, (int)storage[index].tentpos.Y);
                TSPlayer.All.SendTileSquare((int)storage[index].tentpos.X, (int)storage[index].tentpos.Y);
                storage[index].tsplayer.SendInfoMessage("Your tent has been removed!");
                storage.Remove(index);
        }

        private static void DistanceCheck()
        {

            for (int i = 0; i < TShock.Players.Length; i++)
            {
                if (TShock.Players[i] == null)
                    continue;
                if (!storage.ContainsKey(TShock.Players[i].Index))
                    continue;

                TSPlayer player = TShock.Players[i];
                int index = TShock.Players[i].Index;

                if (((player.TileX < storage[index].tentpos.X - 0) || (player.TileX > storage[index].tentpos.X + 0)) || ((player.TileY < storage[index].tentpos.Y - 2) || (player.TileY > storage[index].tentpos.Y + 2)))
                {
                    KillTent(index);
                }
                else // < this is if inside distance>
                {
                    for (int k = 0; k < 3; k++)
                    {
                        if (storage[index].tsplayer.TPlayer.armor[k].netID != 0) //armor check
                        {
                            KillTent(index);
                            return;
                        }
                    }
                    for (int k = 8; k < 11; k++)
                    {
                        if (storage[index].tsplayer.TPlayer.armor[k].netID != 0) //armor check
                        {

                            KillTent(index);
                            return;
                        }
                    }
                    for (int k = 0; k < 16; k++)
                    {
                        if (storage[index].tsplayer.TPlayer.armor[k].wingSlot > 0) //wing check
                        {
                            KillTent(index);
                            return;
                        }
                    }

                    storage[index].tsplayer.SetBuff(2, 100, false);
                    storage[index].tsplayer.SetBuff(6, 100, false);
                    storage[index].tsplayer.SetBuff(106, 100, false);
                    storage[index].tsplayer.SetBuff(80, 100, false);

                    storage[index].tsplayer.SetBuff(10, 10, false);

                    storage[index].lastNearTent = DateTime.Now;
                }
            }
        }


        private void Tent(CommandArgs args)
        {
            if (args.Parameters.Count != 0)
            {
                args.Player.SendErrorMessage("Proper syntax: {0}tent", TShock.Config.CommandSpecifier);
                return;
            }

            if (storage.ContainsKey(args.Player.Index))
            {
                args.Player.SendErrorMessage("You already have an active tent!");
                return;
            }
            var x = args.Player.TileX;
            var y = args.Player.TileY + 2;
            for (int i = x; i < x + 3; i++)
            {
                for (int j = y; j > y - 2; j--)
                {
                    if (Main.tile[i, j].active() && !breakables.Contains(Main.tile[i, j].type))
                    {
                        args.Player.SendErrorMessage("No space here!");
                        return;

                    }
                    WorldGen.KillTile(i, j);
                }
            }
            for (int k = 0; k < 3; k++)
            {
                if (args.Player.TPlayer.armor[k].netID != 0) //armor check
                {
                    args.Player.SendErrorMessage("You must remove your clothes  and wings in order to sleep!");
                    return;
                }
            }
            for (int k = 8; k < 11; k++)
            {
                if (args.Player.TPlayer.armor[k].netID != 0) //armor check2
                {
                    args.Player.SendErrorMessage("You must remove your clothes and wings in order to sleep!");
                    return;
                }
            }
            for (int k = 0; k < 16; k++)
            {
                if (args.Player.TPlayer.armor[k].wingSlot > 0) //wing check
                {
                    args.Player.SendErrorMessage("You must remove your wings in order to sleep!");
                    return;
                }
            }

            WorldGen.PlaceTile(x, y, 187, true, true, -1, 26);
            TSPlayer.All.SendTileSquare(x, y);
            storage.Add(args.Player.Index, new DataStorage(new Vector2(x, y), args.Player));
            args.Player.SendInfoMessage("You have set a tent to sleep!");

        }
    }

    public class DataStorage
    {
        public DateTime lastNearTent;
        public Vector2 tentpos;
        public TSPlayer tsplayer;
        public bool removed;

        public DataStorage(Vector2 _pos, TSPlayer _player)
        {
            tentpos = _pos;
            tsplayer = _player;
            lastNearTent = DateTime.Now;
        }
    }
}
