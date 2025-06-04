namespace BelotWebApp.BelotClasses.Replays
{
    public class BelotReplayDiff
    {
        public BelotReplayDiff()
        {
            Before = new();
            After = new();
        }

        public BelotStateDiff? Before { get; set; }
        public BelotStateDiff? After { get; set; }
    }
}
