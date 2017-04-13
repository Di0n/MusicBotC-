using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TS3QueryLib.Core.Server;

namespace MusicBot
{
    class MusicPlayer
    {
        private QueryRunner queryRunner;
        private Queue<Song> songList;
        public MusicPlayer(ref QueryRunner qr)
        {
            queryRunner = qr;
            songList = new Queue<Song>();
        }

        public void PlaySong(Song song)
        {
            if (Uri.IsWellFormedUriString(song.Url, UriKind.Absolute))
            {
                Process.Start(song.Url);
                CurrentSong = song;
            }
            else
                throw new UriFormatException("Invalid Url");
        }

        public void PlaySong(string url)
        {
            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                Process.Start(url);
                CurrentSong = new Song(url); 
            }
            else
                throw new UriFormatException("Invalid Url");
        }

        public void AddSongToQueue(string url)
        {
            songList.Enqueue(new Song(url));
        }

        public void PlayNextSongInQueue()
        {
            if (songList.Count > 0)
            {
                PlaySong(songList.Dequeue().Url);
            }
        }
        public Song CurrentSong { get; private set; }
        public Song NextSongInQueue 
        { 
            get
            {
                return songList.Peek();
            }
        }
        public Queue<Song> GetQueue { get { return songList; } }
    }

    public class Song
    {
        public Song(string url)
        {
            // find more info about song
            Url = url;
            Started = false;
        }

        public string SongName {get; set;}
        public string Url {get; set;}
        public int Duration { get; set;}
        public bool Started { get; set; }
        public int Time { get; set; }
    }
}
