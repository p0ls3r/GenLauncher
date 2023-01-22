using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SevenZipExtractor;

namespace GenLauncherNet
{
    public static class ModificationsFileHandler
    {
        public static void ExtractModificationFromFiles(List<string> files, string path)
        {
            foreach (var file in files)
            {
                var targetFileName = Path.GetFileName(file);
                var extraction = false;

                var ext = Path.GetExtension(targetFileName).Replace(".", "");

                if (string.Equals(ext, "rar") || string.Equals(ext, "7z") || string.Equals(ext, "zip"))
                    extraction = true;

                Directory.CreateDirectory(path);

                var fileWithTargetPath = Path.Combine(path, targetFileName);

                if (!File.Exists(fileWithTargetPath))
                    File.Copy(file, fileWithTargetPath);

                if (extraction)
                {
                    using (var archiveFile = new ArchiveFile(fileWithTargetPath))
                    {
                        archiveFile.Extract(path);
                    }

                    File.Delete(fileWithTargetPath);
                }
                else
                    if (string.Equals(ext, "big"))
                        File.Move(fileWithTargetPath, Path.ChangeExtension(fileWithTargetPath, ".gib"));
            }
        }
    }
}
