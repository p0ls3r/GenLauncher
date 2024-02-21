using System;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    internal class FTPUpdater : IUpdater
    {
        public event Action<long?, long, double?, ModificationViewModel, string> ProgressChanged;
        public event Action<ModificationViewModel, DownloadResult> Done;
        public DownloadResult DownloadResult { get; set; }
        public ModificationViewModel ModBoxData { get; set; }

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

        public void SetModificationInfo(ModificationViewModel modification)
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

        public DownloadReadiness GetDownloadReadiness()
        {
            throw new NotImplementedException();
        }
    }
}
