namespace BelotWebApp.Services.AppPathService
{
    public class AppPaths : IAppPaths
    {
        private readonly IConfiguration _config;

        public AppPaths(IConfiguration config)
        {
            _config = config;

            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(LogFolder);
            Directory.CreateDirectory(IncompleteGames);
        }

        public string DataFolder =>
            Path.Combine(_config["JassWorkingData"], "data");

        public string LogFolder =>
            Path.Combine(_config["JassWorkingData"], "logs");

        public string IncompleteGames =>
            Path.Combine(LogFolder, "incomplete");

        public string DatabaseFile =>
            Path.Combine(DataFolder, "app.db");
    }
}
