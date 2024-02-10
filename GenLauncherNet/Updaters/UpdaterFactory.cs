using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class UpdaterFactory: IUpdaterFactory
    {
        public UpdaterFactory()
        {
                
        }

        public IUpdater CreateUpdater(ModificationViewModel modification, bool httpSingleFileDownload = false)
        {
            IUpdater updater = null;
            if (string.IsNullOrEmpty(modification.LatestVersion.S3HostLink) ||
                string.IsNullOrEmpty(modification.LatestVersion.S3FolderName) ||
                httpSingleFileDownload)
            {
                updater = new HttpSingleFileUpdater();
            }
            else
            {
                updater = new S3Updater();
            }

            updater.SetModificationInfo(modification);

            return updater;
        }
    }
}
