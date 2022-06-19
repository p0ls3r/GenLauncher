using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class ComboBoxData
    {
        public string VersionName { get; set; }
        public ModificationReposVersion Modification { get; set; }

        public ModBoxData ModBoxData { get; set; }

        public GridControls _GridControls { get; private set; }
        public ComboBoxData(ModificationReposVersion modification, string version, ModBoxData modBoxData)
        {
            Modification = modification;
            VersionName = version;
            ModBoxData = modBoxData;
        }

        public ComboBoxData()
        {

        }
    }
}
