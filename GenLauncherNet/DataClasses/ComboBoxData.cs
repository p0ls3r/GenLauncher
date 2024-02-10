﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class ComboBoxData
    {
        public string VersionName { get; set; }
        public ModificationVersion SelectedVersion { get; set; }

        public ModificationViewModel ModBoxData { get; set; }

        public ComboBoxData(ModificationVersion SelectedModification, string version, ModificationViewModel modBoxData)
        {
            SelectedVersion = SelectedModification;
            VersionName = version;
            ModBoxData = modBoxData;
        }

        public ComboBoxData()
        {

        }
    }
}
