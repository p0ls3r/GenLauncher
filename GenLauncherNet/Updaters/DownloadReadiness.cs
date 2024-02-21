namespace GenLauncherNet
{
    public struct DownloadReadiness
    {
        public bool ReadyToDownload;
        public ErrorType Error;
    }

    public enum ErrorType
    {
        Unknown = 0,
        TimeOutOfSync = 1,
    }
}
