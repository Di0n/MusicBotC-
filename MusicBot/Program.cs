using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TS3QueryLib.Core;
using TS3QueryLib.Core.Common.Responses;
using TS3QueryLib.Core.Server;
using TS3QueryLib.Core.Server.Entities;
using TS3QueryLib.Core.Server.Responses;
using System.Timers;
using TS3QueryLib.Core.CommandHandling;
using TS3QueryLib.Core.Server.Notification.EventArgs;
using IniParser;
using IniParser.Model;

namespace MusicBot
{
    class Program
    {
        #region Program Start
        static void Main(string[] args)
        {
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile("Settings.ini");

            string serverAddress = data["SERVER"]["IP"];
            ushort port;
            if (!ushort.TryParse(data["SERVER"]["Port"], out port))
            {
                Console.WriteLine("Failed to parse port.");
                // TODO logging
                return;
            }

            string name = data["QUERYUSER"]["Name"];
            string password = data["QUERYUSER"]["Password"];
           // const string serverAddress = "62.112.11.135";
            //const ushort port = 10011;
           
            new Program(serverAddress, port, name, password).Run();
        }

        #endregion

        //const string NAME = "Bassment";
        //const string PASSWORD = "fbIG34+M";//"z1aLbSUl";

        private InitialiseHelper    initHelper;

        private AsyncTcpDispatcher  queryDispatcher;
        private QueryRunner         queryRunner;
        private CommandHandler      cmdHandler;
        private MusicPlayer         mPlayer;

        public readonly string serverAddress;
        public readonly ushort port;
        public readonly string queryName;
        public readonly string password;

        public Program(string serverAddress, ushort port, string queryName, string password)
        {
            // Get ip, port, name, password
            this.serverAddress  = serverAddress;
            this.port           = port;
            this.queryName      = queryName;
            this.password       = password;
            initHelper          = new InitialiseHelper();
        }

        private void Run()
        {
            Connect();
            initHelper.Wait();
            if (!initHelper.Success)
            {
                Console.WriteLine("Initialising failed.");
                return;
            }
            initHelper.Dispose();

            WhoAmIResponse resp = queryRunner.SendWhoAmI();

            uint cid = Utils.GetMusicChannelID(ref queryRunner);

            queryRunner.MoveClient(resp.ClientId, cid);

            System.Timers.Timer keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.AutoReset = true;
            keepAliveTimer.Interval = 540; // 9 min
            keepAliveTimer.Elapsed += (o, e) => queryRunner.SendWhoAmI();
            keepAliveTimer.Start();

            while (true)
            {
                Task.Delay(10).Wait();
            }
        }

        private void Connect()
        {
            queryDispatcher = new AsyncTcpDispatcher(serverAddress, port);
            queryDispatcher.ReadyForSendingCommands += QueryDispatcher_ReadyForSendingCommands;
            queryDispatcher.ServerClosedConnection += QueryDispatcher_ServerClosedConnection;
            queryDispatcher.SocketError += QueryDispatcher_SocketError;
            queryDispatcher.Connect();
        }

        void QueryDispatcher_ReadyForSendingCommands(object sender, EventArgs e)
        {
            queryRunner = new QueryRunner(queryDispatcher);

            SimpleResponse loginResp = queryRunner.Login(queryName, password);

            if (loginResp.IsErroneous)
            {
                Console.WriteLine("Failed to login!");
                initHelper.Success = false;
                return;
            }

            VersionResponse vResp = queryRunner.GetVersion();

            if (vResp.IsErroneous)
            {
                Console.WriteLine("Could not get server version!");
                initHelper.Success = false;
                return;
            }


            queryRunner.SelectVirtualServerByPort(9987);
            queryRunner.RegisterForNotifications(ServerNotifyRegisterEvent.TextPrivate);
            queryRunner.RegisterForNotifications(ServerNotifyRegisterEvent.TextChannel);

            queryRunner.Notifications.ChannelMessageReceived += (o, ev) => OnMessageReceived(MessageTarget.Channel, ev);
            queryRunner.Notifications.ClientMessageReceived += (o, ev) => OnMessageReceived(MessageTarget.Client, ev);

            cmdHandler = new CommandHandler(ref queryRunner);
            mPlayer = new MusicPlayer(ref queryRunner);
            Console.WriteLine("Server version:\n\nPlatform: {0}\nVersion: {1}\nBuild: {2}\n", vResp.Platform, vResp.Version, vResp.Build);
            initHelper.Success = true;
        }

        private void OnMessageReceived(MessageTarget target, MessageReceivedEventArgs e)
        {
            if (!e.Message.StartsWith("!") || String.IsNullOrWhiteSpace(e.Message))
                return;

            int index = 0;

            for (int i = 0; i < e.Message.Length; i++)
            {
                if (e.Message[i] == ' ')
                {
                    index = i;
                    break;
                }
            }

            string command = (index != 0) ? e.Message.Substring(0, index) : e.Message;

            uint cid = (target == MessageTarget.Channel) ? Utils.GetMusicChannelID(ref queryRunner) : e.InvokerClientId;
            switch (command)
            {
                case "!mhelp":
                    //uint cid = (target == MessageTarget.Channel) ? Utils.GetMusicChannelID(ref queryRunner) : e.InvokerClientId;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("\n");
                    sb.AppendLine("*** Available Commands ***");
                    sb.AppendLine("!mhelp           Displays help");
                    sb.AppendLine("!play <url>      Starts playing the requested song");
                    sb.AppendLine("!song            Displays the current song");
                    sb.AppendLine("!add <url>       Adds a song to the queue");
                    SendText(target, cid, sb.ToString());
                    break;
                case "!song":
                    SendText(target, cid, mPlayer.CurrentSong.SongName);
                    break;
                case "!next":
                    SendText(target, cid, mPlayer.NextSongInQueue.SongName);
                    break;
                case "!skip":
                    mPlayer.PlayNextSongInQueue();
                    break;
                case "!play":
                case "!add":
                    string url = e.Message.Substring(index);
                    if (!(url.Contains("youtu") || url.Contains("soundcloud")))
                        return;

                    url = url.Trim();
                    url = url.Remove(url.IndexOf("[URL]"), 5);
                    url = url.Remove(url.IndexOf("[/URL]"));

                    if (command == "!play")
                        mPlayer.PlaySong(url);
                    else
                        mPlayer.AddSongToQueue(url);
                    break;
                default:
                    break;
            }
        }


        void QueryDispatcher_SocketError(object sender, TS3QueryLib.Core.Communication.SocketErrorEventArgs e)
        {
        }

        void QueryDispatcher_ServerClosedConnection(object sender, EventArgs e)
        {

        }

        SimpleResponse SendText(MessageTarget target, uint cid, string txt)
        {
            return queryRunner.SendTextMessage(target, cid, txt);
        }

    }
}
