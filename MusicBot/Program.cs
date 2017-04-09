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

namespace MusicBot
{
    class Program
    {
        #region Program Start
        static void Main(string[] args)
        {
            const string serverAddress = "62.112.11.135";
            const ushort port = 10011;

            new Program(serverAddress, port).Run();
        }

        #endregion

        const string NAME = "Dion";
        const string PASSWORD = "z1aLbSUl";

        private InitialiseHelper initHelper;

        private AsyncTcpDispatcher queryDispatcher;
        private QueryRunner queryRunner;
        private CommandHandler cmdHandler;

        public string ServerAddress { get; private set; }
        public ushort Port { get; private set; }

        public Program(string serverAddress, ushort port)
        {
            ServerAddress = serverAddress;
            Port = port;
            initHelper = new InitialiseHelper();
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

            uint cid = Utils.GetMusicChannelID(queryRunner);

            queryRunner.MoveClient(resp.ClientId, cid);

            System.Timers.Timer keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.AutoReset = true;
            keepAliveTimer.Interval = 540; // 9 min
            keepAliveTimer.Elapsed += (o, e) => queryRunner.SendWhoAmI();
            keepAliveTimer.Start();

            Console.ReadLine();
        }

        private void Connect()
        {
            queryDispatcher = new AsyncTcpDispatcher(ServerAddress, Port);
            queryDispatcher.ReadyForSendingCommands += QueryDispatcher_ReadyForSendingCommands;
            queryDispatcher.ServerClosedConnection += QueryDispatcher_ServerClosedConnection;
            queryDispatcher.SocketError += QueryDispatcher_SocketError;
            queryDispatcher.Connect();
        }

        void QueryDispatcher_ReadyForSendingCommands(object sender, EventArgs e)
        {
            queryRunner = new QueryRunner(queryDispatcher);

            SimpleResponse loginResp = queryRunner.Login(NAME, PASSWORD);

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

            string command = e.Message.Substring(0, index);

            switch (command)
            {
                case "!mhelp":
                    uint cid = Utils.GetMusicChannelID(ref queryRunner);
                    cmdHandler.OnHelp(target, cid);
                    break;
                case "!play":
                    string url = 
                    break;
                case "!add":
                    break;
                default:
                    break;
            }
            if (command == "!mhelp")
            {
                uint cid = Utils.GetMusicChannelID(queryRunner);
                cmdHandler.OnHelp(MessageTarget.Channel, cid);
            }
            else if (command.StartsWith("!play"))
            {
                if (!(command.Contains("youtu") || command.Contains("soundcloud")))
                    return;

                url = url.Substring(5);
                url = url.Trim();
                cmdHandler.OnPlay(command);
            }
            else if (command.StartsWith("!add"))
                cmdHandler.OnAdd();
        }


        void QueryDispatcher_SocketError(object sender, TS3QueryLib.Core.Communication.SocketErrorEventArgs e)
        {
        }

        void QueryDispatcher_ServerClosedConnection(object sender, EventArgs e)
        {

        }

    }
}
