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
            initHelper.Dispose();

            WhoAmIResponse resp = queryRunner.SendWhoAmI();

            uint cid = Utils.GetMusicChannelID(queryRunner);

            queryRunner.MoveClient(resp.ClientId, cid);

            System.Timers.Timer keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.AutoReset = true;
            keepAliveTimer.Interval = 540;
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

            queryRunner.Notifications.ChannelMessageReceived += Notifications_ChannelMessageReceived;
            queryRunner.Notifications.ClientMessageReceived += Notifications_ClientMessageReceived;

            Console.WriteLine("Server version:\n\nPlatform: {0}\nVersion: {1}\nBuild: {2}\n", vResp.Platform, vResp.Version, vResp.Build);
            initHelper.Success = true;
        }

        void Notifications_ClientMessageReceived(object sender, TS3QueryLib.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {

        }

        void Notifications_ChannelMessageReceived(object sender, TS3QueryLib.Core.Server.Notification.EventArgs.MessageReceivedEventArgs e)
        {
            if (!e.Message.StartsWith("!"))
                return;

            string command = e.Message;
            if (command == "!mhelp")
                CommandHandler.OnHelp(ref queryRunner);
            else if (command.StartsWith("!pla"))
                CommandHandler.OnPlay(ref queryRunner, e);
        }


        void QueryDispatcher_SocketError(object sender, TS3QueryLib.Core.Communication.SocketErrorEventArgs e)
        {
        }

        void QueryDispatcher_ServerClosedConnection(object sender, EventArgs e)
        {

        }

    }
}
