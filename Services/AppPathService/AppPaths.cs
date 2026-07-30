namespace BelotWebApp.Services.AppPathService
{
    public class AppPaths : IAppPaths
    {
        private readonly IConfiguration _config;
        private readonly string _workingData;

        public AppPaths(IConfiguration config)
        {
            _config = config;
            _workingData = config["JassWorkingData"] ?? throw new InvalidOperationException("Configuration value 'JassWorkingData' is missing.");

            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(HubLogFolder); // creates LogFolder
            Directory.CreateDirectory(CleanupLogFolder);
            Directory.CreateDirectory(IncompleteGameFolder); // creates Replays
        }

        public string DataFolder => Path.Combine(_workingData, "data");

        public string DatabaseFile => Path.Combine(DataFolder, "app.db");

        public string LogFolder => Path.Combine(_workingData, "logs");

        public string HubLogFolder => Path.Combine(LogFolder, "hub");

        public string CleanupLogFolder => Path.Combine(LogFolder, "cleanup");

        public string ReplayFolder => Path.Combine(_workingData, "replays");

        public string IncompleteGameFolder => Path.Combine(ReplayFolder, "incomplete");
    }
}
