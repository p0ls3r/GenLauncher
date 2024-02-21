using System;
using System.IO;

namespace GenLauncherNet
{
    public class ModificationFileInfo
    {
        public string FileName;
        public string Hash;
        public ulong Size;

        public ModificationFileInfo(string name, string hash)
        {
            FileName = name;
            Hash = hash;
        }

        public ModificationFileInfo(string name, string hash, ulong size)
        {
            FileName = name;
            Hash = hash;
            Size = size;
        }

        public override bool Equals(object obj)
        {
            if (obj is ModificationFileInfo modInfo && String.Equals(Hash, modInfo.Hash, StringComparison.OrdinalIgnoreCase))
            {
                if (String.Equals(FileName, modInfo.FileName, StringComparison.OrdinalIgnoreCase))
                    return true;

                var filename1 = Path.ChangeExtension(modInfo.FileName, "");
                var filename2 = Path.ChangeExtension(FileName, "");

                if (String.Equals(filename1, filename2, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
                
            return false;
        }

        public override int GetHashCode()
        {
            return (FileName.ToUpper() + Hash.ToUpper()).GetHashCode();
        }
    }
}
