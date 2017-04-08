using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3QueryLib.Core.Server;

namespace MusicBot
{
    class Utils
    {
        public static uint GetMusicChannelID(QueryRunner qr)
        {
            var cl = qr.GetChannelList(true);
            return cl.First(c => c.Topic == "music_channel").ChannelId;
        }
    }
}
