using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public struct DownloadReadiness
    {
        public bool ReadyToDownload;
        public ErrorType Error;
    }

    public enum ErrorType
    {
        Unknown = 0,
        TimeOutOfSync = 1,
    }
}
