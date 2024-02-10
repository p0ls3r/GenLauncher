using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public interface IUpdaterFactory
    {
        IUpdater CreateUpdater(ModificationViewModel modification, bool httpSingleFileDownload = false);
    }
}
