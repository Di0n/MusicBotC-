﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3QueryLib.Core.Server;

namespace MusicBot
{
    class MusicPlayer
    {
        private QueryRunner queryRunner;
        public MusicPlayer(ref QueryRunner qr)
        {
            queryRunner = qr;
        }

        public void PlaySong()
        {

        }

        public void AddSong()
        {

        }

        public string CurrentSong { get; private set; }
    }
}
