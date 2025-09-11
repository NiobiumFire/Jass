namespace BelotWebApp.Services.AppPathService
{
    public interface IAppPaths
    {
        string DataFolder { get; }
        string LogFolder { get; }
        string IncompleteGames { get; }
        string DatabaseFile { get; }
    }
}
