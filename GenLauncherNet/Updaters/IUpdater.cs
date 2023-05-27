using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public interface IUpdater
    {
        event Action<long?, long, double?, ModificationContainer, string> ProgressChanged;
        event Action<ModificationContainer, DownloadResult> Done;
        ModificationContainer ModBoxData { get; }
        Task StartDownloadModification();
        void CancelDownload();
        void SetModificationInfo(ModificationContainer modification);
        DownloadResult GetResult();
        void Dispose();
    }
}
