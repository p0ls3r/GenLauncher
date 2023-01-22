using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GenLauncherNet
{
    public class GameOptionsHandler
    {
        public Dictionary<string, string> gameOptions { get; set; }
        private string optionsFilePath;

        public GameOptionsHandler()
        {
            gameOptions = new Dictionary<string, string>();

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                optionsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data" + "/Options.ini";
            else
                optionsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data" + "/Options.ini";

            CheckGameOptionsFile();
            ReadOptions();
            ValidateOptions();
        }

        public void SaveOptions()
        {
            File.WriteAllLines(optionsFilePath, gameOptions.Select(t => t.Key + " =" + (t.Value[0] == ' ' ? t.Value : " " + t.Value)));
        }

        public void ApplyDefaultGameOptions()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth.ToString();
            var screenHeight = SystemParameters.PrimaryScreenHeight.ToString();

            gameOptions["Resolution"] = " " + String.Format("{0} {1}", screenWidth, screenHeight);

            gameOptions["HeatEffects"] = " no";
            gameOptions["UseCloudMap"] = " no";
            gameOptions["UseShadowVolumes"] = " no";
            gameOptions["MaxParticleCount"] = " 2500";
            gameOptions["UseShadowDecals"] = " yes";
            gameOptions["DynamicLOD"] = " yes";

            SaveOptions();
        }

        private void CheckGameOptionsFile()
        {
            var folderPath = string.Empty;

            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Zero Hour Data";
            else
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Command and Conquer Generals Data";

            if (!OptionsFileExists(folderPath))
                ExtractOptions(folderPath);
        }

        private bool OptionsFileExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = folderPath + "\\Options.ini";

            return File.Exists(filePath);
        }

        private void ExtractOptions(string folderPath)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("GenLauncherNet.Options.options.ini"))
            {
                using (var file = new FileStream(folderPath + "/Options.ini", FileMode.Create, FileAccess.Write))
                {
                    resource?.CopyTo(file);
                }
            }
        }

        private void ValidateOptions()
        {
            if (!gameOptions.ContainsKey("Resolution"))
            {
                gameOptions.Add("Resolution", "1024×768");
            }

            if (!gameOptions.ContainsKey("MaxParticleCount"))
            {
                gameOptions.Add("MaxParticleCount", "2500");
            }

            if (!gameOptions.ContainsKey("TextureReduction"))
            {
                gameOptions.Add("TextureReduction", "1");
            }

            gameOptions.TryAdd("UseShadowVolumes", " no");
            gameOptions.TryAdd("BuildingOcclusion", " no");
            gameOptions.TryAdd("UseShadowDecals", " no");
            gameOptions.TryAdd("ShowTrees", " no");
            gameOptions.TryAdd("UseCloudMap", " no");
            gameOptions.TryAdd("ExtraAnimations", " no");
            gameOptions.TryAdd("UseLightMap", " no");
            gameOptions.TryAdd("DynamicLOD", " yes");
            gameOptions.TryAdd("ShowSoftWaterEdge", " no");
            gameOptions.TryAdd("HeatEffects", " no");
            gameOptions.TryAdd("UseAlternateMouse", " no");
        }

        private void ReadOptions()
        {
            foreach (string line in File.ReadLines(optionsFilePath))
            {
                if (!line.Contains('='))
                    continue;

                var key = line.Split('=')[0].Replace(" ", String.Empty);
                var value = line.Split('=')[1];

                if (String.IsNullOrEmpty(value))
                    continue;

                if (!gameOptions.ContainsKey(key))
                    gameOptions.Add(key, value);
            }
        }
    }

    public static class DictionaryExtension
    {
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }
    }
}
