using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    internal class FTPUpdater : IUpdater
    {
        public event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        public event Action<ModificationContainer, DownloadResult> Done;
        public DownloadResult DownloadResult { get; set; }
        public ModificationContainer ModBoxData { get; set; }

        public FTPUpdater()
        {
             throw new NotImplementedException();
        }

        public void StartDownloadModification()
        {
            throw new NotImplementedException();
        }

        Task IUpdater.StartDownloadModification()
        {
            throw new NotImplementedException();
        }

        public void CancelDownload()
        {
            throw new NotImplementedException();
        }

        public void SetModificationInfo(ModificationContainer modification)
        {
            throw new NotImplementedException();
        }

        public DownloadResult GetResult()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
