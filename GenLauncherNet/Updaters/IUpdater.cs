using System;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public interface IUpdater
    {
        event Action<long?, long, double?, ModificationViewModel, string> ProgressChanged;
        event Action<ModificationViewModel, DownloadResult> Done;
        ModificationViewModel ModBoxData { get; }
        DownloadReadiness GetDownloadReadiness();
        Task StartDownloadModification();
        void CancelDownload();
        void SetModificationInfo(ModificationViewModel modification);
        DownloadResult GetResult();
        void Dispose();
    }
}
