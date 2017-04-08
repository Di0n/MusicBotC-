using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3QueryLib.Core.Server;
using TS3QueryLib.Core.Server.Notification.EventArgs;

namespace MusicBot
{
    class CommandHandler
    {
        public static void OnHelp(ref QueryRunner qr)
        {
            uint cid = Utils.GetMusicChannelID(qr);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n");
            sb.AppendLine("*** Available Commands ***");
            sb.AppendLine("!mhelp           Displays help");
            sb.AppendLine("!play <url>      Starts playing the requested song");
            sb.AppendLine("!song            Displays the current song");
            sb.AppendLine("!add <url>       Adds a song to the queue");
           
            qr.SendTextMessage(TS3QueryLib.Core.CommandHandling.MessageTarget.Channel, cid, sb.ToString());
        }

        public static void OnPlay(ref QueryRunner qr, MessageReceivedEventArgs e)
        {
            if (!(e.Message.Contains("youtu") || e.Message.Contains("soundcloud")))
                return;

            string url = e.Message.Substring(5);
            url.Trim();

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                Process.Start(url);
        }

        public static void OnSong(ref QueryRunner qr)
        {

        }
    }
}
