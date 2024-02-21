using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GenLauncherNet.Windows;

namespace GenLauncherNet
{
    /// <summary>
    /// Логика взаимодействия для InitWindow.xaml
    /// </summary>
    public partial class InitWindow : Window
    {
        private HttpClient client = new HttpClient();

        public InitWindow()
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GenLauncher", "1"));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => CreateLauncherFolders());

            var connected = await PrepareLauncher();
            EntryPoint.SessionInfo.Connected = connected;
            
            this.Hide();
            MainWindow mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            this.Close();
        }

        public async Task<bool> PrepareLauncher()
        {            
            await Task.Run(() => GameLauncher.RenameGameFilesToOriginalState());

            await Task.Run(() => EntryPoint.DeleteTempFolders(new DirectoryInfo(Directory.GetCurrentDirectory())));
            await Task.Run(() => DeleteOldGenLauncherFile());

            SetSessionInfo();
            SetColorsInfo();

            await Task.Run(() => CheckForCustomVisualInfo());
            await Task.Run(() => CheckForCustomBG());
            await Task.Run(() => EntryPoint.ReplaceDlls());

            if (!Directory.Exists(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder)))
                Directory.CreateDirectory(Path.Combine(EntryPoint.LauncherFolder, EntryPoint.LauncherImageSubFolder));

            await Task.Run(() => Unpacker.ExctractImages());
            await Task.Run(() => Unpacker.ExtractDlls());


            var connected = await CheckConnection();

            await DataHandler.InitData(connected);

            if (DataHandler.ReposModsNames == null)
                connected = false;

            if (connected)
            {
                var lastActivatedMod = DataHandler.GetSelectedMod();

                if (lastActivatedMod != null)
                    await DataHandler.ReadPatchesAndAddonsForMod(lastActivatedMod);
            }
            else
                MessageBox.Show(LocalizedStrings.Instance["CannotConnect"]);

            client.Dispose();

            //var patch = DataHandler

            return connected;
        }

        private static void FillZHFiles()
        {
            EntryPoint.GameFiles.Add("AudioChineseZH.big".ToLower());
            EntryPoint.GameFiles.Add("Gensec.big".ToLower());
            EntryPoint.GameFiles.Add("AudioEnglishZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioFrenchZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioGermanZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioItalianZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioKoreanZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioPolishZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioSpanishZH.big".ToLower());
            EntryPoint.GameFiles.Add("AudioZH.big".ToLower());
            EntryPoint.GameFiles.Add("BrazilianZH.big".ToLower());
            EntryPoint.GameFiles.Add("ChineseZH.big".ToLower());
            EntryPoint.GameFiles.Add("EnglishZH.big".ToLower());
            EntryPoint.GameFiles.Add("FrenchZH.big".ToLower());
            EntryPoint.GameFiles.Add("GermanZH.big".ToLower());
            EntryPoint.GameFiles.Add("ItalianZH.big".ToLower());
            EntryPoint.GameFiles.Add("KoreanZH.big".ToLower());
            EntryPoint.GameFiles.Add("PolishZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpanishZH.big".ToLower());
            EntryPoint.GameFiles.Add("GensecZH.big".ToLower());
            EntryPoint.GameFiles.Add("INIZH.big".ToLower());
            EntryPoint.GameFiles.Add("MapsZH.big".ToLower());
            EntryPoint.GameFiles.Add("Music.big".ToLower());
            EntryPoint.GameFiles.Add("MusicZH.big".ToLower());
            EntryPoint.GameFiles.Add("PatchZH.big".ToLower());
            EntryPoint.GameFiles.Add("ShadersZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechBrazilianZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechChineseZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechEnglishZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechFrenchZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechGermanZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechItalianZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechKoreanZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechPolishZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechSpanishZH.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechZH.big".ToLower());
            EntryPoint.GameFiles.Add("TerrainZH.big".ToLower());
            EntryPoint.GameFiles.Add("TexturesZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DEnglishZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DGermanZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DChineseZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DGerman2ZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DItalianZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DKoreanZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DPolishZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DSpanishZH.big".ToLower());
            EntryPoint.GameFiles.Add("W3DZH.big".ToLower());
            EntryPoint.GameFiles.Add("WindowZH.big".ToLower());
        }

        private static void FillGeneralsFiles()
        {
            EntryPoint.GameFiles.Add("Audio.big".ToLower());
            EntryPoint.GameFiles.Add("AudioBrazilian.big".ToLower());
            EntryPoint.GameFiles.Add("AudioChinese.big".ToLower());
            EntryPoint.GameFiles.Add("AudioEnglish.big".ToLower());
            EntryPoint.GameFiles.Add("AudioFrench.big".ToLower());
            EntryPoint.GameFiles.Add("AudioGerman.big".ToLower());
            EntryPoint.GameFiles.Add("AudioGerman2.big".ToLower());
            EntryPoint.GameFiles.Add("AudioItalian.big".ToLower());
            EntryPoint.GameFiles.Add("AudioKorean.big".ToLower());
            EntryPoint.GameFiles.Add("AudioPolish.big".ToLower());
            EntryPoint.GameFiles.Add("AudioSpanish.big".ToLower());
            EntryPoint.GameFiles.Add("Brazilian.big".ToLower());
            EntryPoint.GameFiles.Add("Chinese.big".ToLower());
            EntryPoint.GameFiles.Add("English.big".ToLower());
            EntryPoint.GameFiles.Add("French.big".ToLower());
            EntryPoint.GameFiles.Add("German.big".ToLower());
            EntryPoint.GameFiles.Add("German2.big".ToLower());
            EntryPoint.GameFiles.Add("Italian.big".ToLower());
            EntryPoint.GameFiles.Add("Korean.big".ToLower());
            EntryPoint.GameFiles.Add("Polish.big".ToLower());
            EntryPoint.GameFiles.Add("Spanish.big".ToLower());
            EntryPoint.GameFiles.Add("gensec.big".ToLower());
            EntryPoint.GameFiles.Add("INI.big".ToLower());
            EntryPoint.GameFiles.Add("maps.big".ToLower());
            EntryPoint.GameFiles.Add("Music.big".ToLower());
            EntryPoint.GameFiles.Add("Patch.big".ToLower());
            EntryPoint.GameFiles.Add("shaders.big".ToLower());
            EntryPoint.GameFiles.Add("Speech.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechBrazilian.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechChinese.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechEnglish.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechFrench.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechGerman.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechGerman2.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechItalian.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechKorean.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechPolish.big".ToLower());
            EntryPoint.GameFiles.Add("SpeechSpanish.big".ToLower());
            EntryPoint.GameFiles.Add("W3DChinese.big".ToLower());
            EntryPoint.GameFiles.Add("W3DGerman2.big".ToLower());
            EntryPoint.GameFiles.Add("W3DItalian.big".ToLower());
            EntryPoint.GameFiles.Add("W3DKorean.big".ToLower());
            EntryPoint.GameFiles.Add("W3DPolish.big".ToLower());
            EntryPoint.GameFiles.Add("W3DSpanish.big".ToLower());
            EntryPoint.GameFiles.Add("Terrain.big".ToLower());
            EntryPoint.GameFiles.Add("Textures.big".ToLower());
            EntryPoint.GameFiles.Add("W3D.big".ToLower());
            EntryPoint.GameFiles.Add("Window.big".ToLower());
        }
        
        private static void DeleteOldGenLauncherFile()
        {
            var ExutetableFileName = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            var tempFile = ExutetableFileName + "Old";

            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                //TODO logger
            }
        }

        private static void SetColorsInfo()
        {
            if (EntryPoint.SessionInfo.GameMode == Game.ZeroHour)
            {
                EntryPoint.DefaultColors = new ColorsInfo("#00e3ff", "DarkGray", "#7a7db0", "#baff0c", "#232977", "#090502",
                    "#B3000000", "White", "#090502", "#F21d2057", "#F21d2057", "#2534ff");
                EntryPoint.DefaultColors.GenLauncherBackgroundImage = new ImageBrush(
                    new BitmapImage(new Uri("pack://application:,,,/GenLauncher;component/Images/Background.png")));
            }
            else
            {
                EntryPoint.DefaultColors = new ColorsInfo("#ffbb00", "DarkGray", "#ffbb00", "#ffbb00", "#e24c17", "#090502",
                    "#B3000000", "White", "#090502", "#5a210d", "#8a2e0d", "#e24c17");
                EntryPoint.DefaultColors.GenLauncherBackgroundImage = new ImageBrush(
                    new BitmapImage(
                        new Uri("pack://application:,,,/GenLauncher;component/Images/BackgroundGenerals.png")));
            }

            EntryPoint.Colors = EntryPoint.DefaultColors;
        }

        private static void CheckForCustomBG()
        {
            if (!File.Exists("GlBg.png"))
                return;

            EntryPoint.DefaultColors.GenLauncherBackgroundImage = new ImageBrush(new BitmapImage(
                new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), @"GlBg.png"), UriKind.Absolute)));
        }

        private static void CheckForCustomVisualInfo()
        {
            if (!File.Exists("Colors.yaml"))
                return;

            var deSerializer = new YamlDotNet.Serialization.Deserializer();

            ColorsInfoString colors = new ColorsInfoString();

            using (FileStream fstream = new FileStream("Colors.yaml", FileMode.OpenOrCreate))
            {
                colors = deSerializer.Deserialize<ColorsInfoString>(new StreamReader(fstream));
            }

            if (colors != null)
                EntryPoint.DefaultColors = new ColorsInfo(colors);
        }

        private static void SetSessionInfo()
        {
            EntryPoint.SessionInfo = new SessionInformation();

            if (File.Exists("WindowZH.big"))
            {
                EntryPoint.SessionInfo.GameMode = Game.ZeroHour;
                EntryPoint.ModsRepos =
                    EntryPoint.ZHRepos;
                FillZHFiles();
                return;
            }

            if (File.Exists("Window.big"))
            {
                EntryPoint.SessionInfo.GameMode = Game.Generals;
                EntryPoint.ModsRepos =
                    EntryPoint.GenRepos;
                FillGeneralsFiles();
                return;
            }
        }

        private async Task<bool> CheckConnection()
        {
            try
            {
                var response = await client.GetAsync(EntryPoint.ModsRepos, HttpCompletionOption.ResponseContentRead);
                response.EnsureSuccessStatusCode();
            }
            catch 
            {
                return false;
            }
            return true;
        }

        private static void CreateLauncherFolders()
        {
            CreateFolder(EntryPoint.GenLauncherModsFolder);
            CreateFolder(EntryPoint.VulkanDllsFolderName);
        }

        private static void CreateFolder(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);

            if (Directory.Exists(EntryPoint.GenLauncherModsFolderOld))
            {
                MoveContentsOfDirectory(EntryPoint.GenLauncherModsFolderOld, folderName);
            }
        }

        private static void MoveContentsOfDirectory(string source, string target)
        {
            foreach (var file in Directory.EnumerateFiles(source))
            {
                var dest = Path.Combine(target, Path.GetFileName(file));
                File.Move(file, dest);
            }

            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                var dest = Path.Combine(target, Path.GetFileName(dir));
                Directory.Move(dir, dest);
            }

            Directory.Delete(source);
        }
    }
}
