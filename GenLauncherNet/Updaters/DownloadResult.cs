using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public struct DownloadResult
    {
        public bool Crashed;
        public bool Canceled;
        public bool TimedOut;
        public string Message;
    }
}
