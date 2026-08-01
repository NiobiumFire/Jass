using BelotWebApp.Services.AppPathService;
using System.Text.Json;

namespace BelotWebApp.Notification
{
    public class ServerNotificationManager
    {
        private readonly string _filePath;
        private ServerNotification _current = new();
        private readonly object _lock = new();

        public ServerNotificationManager(IAppPaths appPaths)
        {
            _filePath = Path.Combine(appPaths.DataFolder, "notification.json");
            Load();
        }

        public ServerNotification Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }

        public bool NotificationIsActive
        {
            get
            {
                var notification = Current;
                return notification.Enabled && DateTime.UtcNow < notification.ScheduledUtc + TimeSpan.FromDays(1);
            }
        }

        public void Update(ServerNotification notification)
        {
            lock (_lock)
            {
                _current = notification;
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
                var json = JsonSerializer.Serialize(notification);
                File.WriteAllText(_filePath, json);
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _current = JsonSerializer.Deserialize<ServerNotification>(json) ?? new();
                }
            }
            catch (Exception)
            {
                _current = new();
            }
        }
    }
}
