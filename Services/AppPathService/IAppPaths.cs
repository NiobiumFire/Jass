namespace BelotWebApp.Services.AppPathService
{
    public interface IAppPaths
    {
        string DataFolder { get; }
        string DatabaseFile { get; }
        string LogFolder { get; }
        string HubLogFolder { get; }
        string ReplayFolder { get; }
        string IncompleteGameFolder { get; }

    }
}
