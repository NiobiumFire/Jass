namespace BelotWebApp.BelotClasses
{
    public class RoundSummaryGate
    {
        private readonly object _lock = new();
        private TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly HashSet<string> _continueVotes = [];
        private Guid _token;
        private int _requiredContinueVotes;
        public int RoundSummaryDelay { get; set; } = 10000;
        public event Action<int, int>? ContinueVotesUpdated; // (current continue votes, required continue votes)

        public Guid BeginRoundSummary(int expectedHumanCount)
        {
            lock (_lock)
            {
                _token = Guid.NewGuid();
                _continueVotes.Clear();
                _requiredContinueVotes = expectedHumanCount;
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _token;
            }
        }

        public void RegisterContinueVote(string playerId, Guid token) // Room.cs ensures only players count for this, not spectators
        {
            int current, expected;
            lock (_lock)
            {
                if (token != _token) return;
                _continueVotes.Add(playerId);
                current = _continueVotes.Count;
                expected = _requiredContinueVotes;
                CheckComplete();
            }
            ContinueVotesUpdated?.Invoke(current, expected);
        }

        // Server-initiated - no token needed, always applies to whatever round is currently active
        public void RegisterDisconnect(string playerId)
        {
            int current, expected;
            lock (_lock)
            {
                _continueVotes.Add(playerId);
                current = _continueVotes.Count;
                expected = _requiredContinueVotes;
                CheckComplete();
            }
            ContinueVotesUpdated?.Invoke(current, expected);
        }

        private void CheckComplete()
        {
            if (_continueVotes.Count >= _requiredContinueVotes)
            {
                _tcs.TrySetResult(true);
            }
        }

        public Task WaitAsync() => Task.WhenAny(_tcs.Task, Task.Delay(RoundSummaryDelay));
    }
}
