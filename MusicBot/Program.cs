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
using System.Runtime.InteropServices;
using System.Reflection;

namespace MusicBot
{
    class Program
    {
        #region Program Start
        [STAThread]
        static void Main(string[] args)
        {
            bool newInstance;
            string guid = ((GuidAttribute)Assembly.GetExecutingAssembly().
            GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
            string mutexID = String.Format("Global\\{{{0}}}", guid);
            using (Mutex mutex = new Mutex(true, mutexID, out newInstance))
            {
                if (newInstance)
                {
                    new Program().Run();
                }
                else
                    Console.WriteLine("MusicBot is already running!");

                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ApplicationException) { }
                catch (ObjectDisposedException) { }
            } 
        }

        #endregion

        private InitialiseHelper    initHelper;
        private AsyncTcpDispatcher  queryDispatcher;
        private QueryRunner         queryRunner;
        private CommandHandler      cmdHandler;

        private string serverAddress;
        private ushort port;
        private string queryName;
        private string password;

        public Program() { }

        private bool Initialize()
        {
            initHelper = new InitialiseHelper();
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile("Settings.ini");

            serverAddress = data["SERVER"]["IP"];

            if (!ushort.TryParse(data["SERVER"]["Port"], out port))
            {
                Console.WriteLine("Failed to parse port.");
                // TODO logging
                return false;
            }

  
            queryName = data["QUERYUSER"]["Name"];
            password = data["QUERYUSER"]["Password"];

            queryDispatcher = new AsyncTcpDispatcher(serverAddress, port);
            queryDispatcher.ServerClosedConnection += QueryDispatcher_ServerClosedConnection;
            queryDispatcher.SocketError += QueryDispatcher_SocketError;
            queryDispatcher.ReadyForSendingCommands += QueryDispatcher_ReadyForSendingCommands;
            queryDispatcher.Connect();
            initHelper.Wait();

            if (!initHelper.Success)
                return false;

            initHelper.Dispose();

            WhoAmIResponse resp = queryRunner.SendWhoAmI();

            uint cid = Utils.GetMusicChannelID(ref queryRunner);

            queryRunner.MoveClient(resp.ClientId, cid);

            System.Timers.Timer keepAliveTimer = new System.Timers.Timer();
            keepAliveTimer.AutoReset = true;
            keepAliveTimer.Interval = 540; // 9 min
            keepAliveTimer.Elapsed += (o, e) => queryRunner.SendWhoAmI();
            keepAliveTimer.Start();

            return true;
        }

        private void Run()
        {
            #region Initialize
            if (!Initialize())
            {
                Console.WriteLine("Initializing failed.");
                return;
            }
            #endregion

            //*** Main Loop ***//
            while (true) 
            {
                Task.Delay(10).Wait(); // Yield
            }
        }

        private void OnMessageReceived(MessageTarget target, MessageReceivedEventArgs e)
        {
            if (!e.Message.StartsWith("!") || String.IsNullOrWhiteSpace(e.Message))
                return;

            cmdHandler.HandleCommand(target, e);
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
            //mPlayer = new MusicPlayer(ref queryRunner);
            Console.WriteLine("Server version:\n\nPlatform: {0}\nVersion: {1}\nBuild: {2}\n", vResp.Platform, vResp.Version, vResp.Build);
            initHelper.Success = true;
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
