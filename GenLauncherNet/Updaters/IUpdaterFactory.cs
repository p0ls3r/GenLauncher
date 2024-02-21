namespace GenLauncherNet
{
    public interface IUpdaterFactory
    {
        IUpdater CreateUpdater(ModificationViewModel modification, bool httpSingleFileDownload = false);
    }
}
