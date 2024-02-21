using System;
using System.IO;

namespace GenLauncherNet
{
    public static class FilesHandler
    {
        private static string startPath = Directory.GetCurrentDirectory();

        public static void ApplyActionsToGameFiles(params Action<FileInfo>[] actions)
        {
            ApplyActionsToGameFiles(new DirectoryInfo(startPath), actions);
        }

        public static void ApplyActionsToGameFiles(DirectoryInfo directoryInfo, params Action<FileInfo>[] actions)
        {
            foreach (var file in directoryInfo.GetFiles())
            {
                try
                {
                    foreach(var action in actions)
                    {
                        action(file);
                    }
                }
                catch
                {
                    //TODO logger
                }
            }

            foreach (var dirInfo in directoryInfo.GetDirectories())
                ApplyActionsToGameFiles(dirInfo, actions);
        }
    }
}
