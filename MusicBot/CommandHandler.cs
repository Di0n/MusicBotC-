using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3QueryLib.Core.CommandHandling;
using TS3QueryLib.Core.Common.Responses;
using TS3QueryLib.Core.Server;
using TS3QueryLib.Core.Server.Notification.EventArgs;

namespace MusicBot
{
    class CommandHandler
    {
        private QueryRunner qr;
        public CommandHandler(ref QueryRunner qr)
        {
            this.qr = qr;
        }
        
        /// <summary>
        /// OnHelp() Command
        /// </summary>
        /// <param name="target">Target can either be a channel or a user.</param>
        /// <param name="cid">Receiver of the help text, can be a channel or a user, depends on target.</param>
        /// <returns>SimpleResponse</returns>
        public SimpleResponse OnHelp(MessageTarget target,uint cid)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n");
            sb.AppendLine("*** Available Commands ***");
            sb.AppendLine("!mhelp           Displays help");
            sb.AppendLine("!play <url>      Starts playing the requested song");
            sb.AppendLine("!song            Displays the current song");
            sb.AppendLine("!add <url>       Adds a song to the queue");
           
            
            return qr.SendTextMessage(target, cid, sb.ToString());
        }

        public void OnPlay(string url)
        {
            if (!(url.Contains("youtu") || url.Contains("soundcloud")))
                return;

            url = url.Substring(5);
            url = url.Trim();

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                Process.Start(url);
        }

        public SimpleResponse OnSong(MessageTarget target, uint cid)
        {
            // mchannel.getCurrentSong()
            return null;
        }

        public void OnAdd()
        {

        }

        
    }
}
