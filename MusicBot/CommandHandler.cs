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
        private MusicPlayer mp;
        public CommandHandler(ref QueryRunner qr)
        {
            this.qr = qr;
            mp = new MusicPlayer();
        }
        
        /// <summary>
        /// OnHelp() Command
        /// </summary>
        /// <param name="target">Target can either be a channel or a user.</param>
        /// <param name="cid">Receiver of the help text, can be a channel or a user, depends on target.</param>
        /// <returns>SimpleResponse</returns>
        
        public void HandleCommand(MessageTarget target, MessageReceivedEventArgs e)
        {
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
            string param = (index != 0) ? e.Message.Substring(index) : String.Empty;

            uint cid = (target == MessageTarget.Channel) ? Utils.GetMusicChannelID(ref qr) : e.InvokerClientId;

            
            switch (command)
            {
                case "!mhelp":
                    OnHelp(target, cid);
                    break;

                case "!song":
                    qr.SendTextMessage(target, cid, mp.CurrentSong.SongName);
                    break;

                case "!next":
                    qr.SendTextMessage(target, cid, mp.NextSongInQueue.SongName);
                    break;

                case "!skip":
                    mp.PlayNextSongInQueue();
                    break;

                case "!play":
                case "!add":
                    OnRequestSong(command, param, target, cid);
                    break;
             
                default:
                    break;
            }
        }

        private SimpleResponse OnHelp(MessageTarget target,uint cid)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("0x20");
            sb.AppendLine("*** Available Commands ***");
            sb.AppendLine("!help           Displays help");
            sb.AppendLine("!play <url>      Starts playing the requested song");
            sb.AppendLine("!add <url>       Adds a song to the queue");
            sb.AppendLine("!song            Displays the current song");
            return qr.SendTextMessage(target, cid, sb.ToString());
        }

        private void OnRequestSong(string command, string param, MessageTarget target, uint cid) // Play Add
        {
            string url = param;
            if (!(url.Contains("youtu") || url.Contains("soundcloud")))
            {
                string textToSend = "Only Soundcloud & Youtube links are allowed!";
                qr.SendTextMessage(target, cid, textToSend);
                return;
            }

            url = url.Trim();
            url = url.Remove(url.IndexOf("[URL]"), 5);
            url = url.Remove(url.IndexOf("[/URL]"));

            if (command == "!play")
                mp.PlaySong(url);
            else
                mp.AddSongToQueue(url);
        }
    }
}
