namespace GenLauncherNet
{
    public class SessionInformation
    {
        public bool Connected { get; set; }

        public Game GameMode { get; set; }
    }

    public enum Game
    {
        ZeroHour = 0,
        Generals = 1
    }
}
