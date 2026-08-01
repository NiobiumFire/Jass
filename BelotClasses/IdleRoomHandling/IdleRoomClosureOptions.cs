namespace BelotWebApp.BelotClasses.IdleRoomHandling
{
    public class IdleRoomClosureOptions
    {
        public TimeSpan IdleTimeLimit { get; set; } = TimeSpan.FromMinutes(30);
        public TimeSpan GracePeriodAfterWarning { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(60);
    }
}
