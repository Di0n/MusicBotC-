using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicBot
{
    public enum Command
    {
        OnPlay,
        OnSong,
        OnHelp
    }

    public enum Target
    {
        Channel,
        Client
    }
}
