using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class S3Updater : IUpdater
    {
        public event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        public event Action<ModificationContainer, DownloadResult> Done;
        public DownloadResult DownloadResult { get; set; }
        public ModificationContainer ModBoxData { get; set; }

        public S3Updater()
        {
            throw new NotImplementedException();
        }

        public async Task StartDownloadModification()
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
