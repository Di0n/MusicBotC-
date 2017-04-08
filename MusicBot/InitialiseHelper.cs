using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MusicBot
{
    class InitialiseHelper : IDisposable
    {
        private readonly ManualResetEvent done;
        public InitialiseHelper()
        {
            done = new ManualResetEvent(false);
            success = false;
            
        }
        ~InitialiseHelper()
        {
            Dispose();
        }

        private bool disposed = false;
        public virtual void Dispose()
        {
            if (!disposed)
            {
                done.Dispose();
                disposed = true;
            }
        }
        public void Wait()
        {
            done.WaitOne();
        }

        private bool success;
        public bool Success
        {
            get
            {
                return success;
            }
            set
            {
                success = value;
                done.Set();
            }
        }
    }
}
