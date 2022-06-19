using GenLauncherNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SymbolicLinkSupport;

namespace GenLauncherNet
{
    public static class GameFilesHandler
    {
        private static string scriptPath = "Data\\Scripts\\";
        private static string iniPath = "Data\\INI\\";
        private static string[] scbFiles = new string[] { scriptPath + "MultiplayerScripts", scriptPath + "SkirmishScripts", };
        private static string[] iniFiles = new string[] { scriptPath + "Scripts", iniPath + "GameData" };

        public static void DeactivateGameFiles()
        {
            Switch(scbFiles, "scb", "nope");
            Switch(iniFiles, "ini", "nope");
        }

        public static void ActivateGameFilesBack()
        {
            Switch(scbFiles, "nope", "scb");
            Switch(iniFiles, "nope", "ini");
        }

        private static void Switch(string[] source, string from, string to)
        {
            foreach (var file in source)
            {
                if (File.Exists(file + "." + from))
                {
                    if (File.Exists(file + "." + from) && !File.Exists(file + "." + to))
                        File.Move(file + "." + from, file + "." + to);
                    else
                        if (File.Exists(file + "." + to))
                        File.Delete(file + "." + from);
                }
            }
        }
    }
}
