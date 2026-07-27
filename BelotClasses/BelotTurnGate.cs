namespace BelotWebApp.BelotClasses
{
    public class BelotTurnGate
    {
        private TaskCompletionSource<bool>? _taskCompletionSource;
        private CancellationTokenSource? _cancellationTokenSource;
        private DateTimeOffset? _startedAt;
        public bool Waiting => _taskCompletionSource?.Task.IsCompleted == false;
        public double? ElapsedTime => _startedAt == null ? null : (DateTimeOffset.UtcNow - _startedAt.Value).TotalSeconds;

        public bool Signal()
        {
            if (_taskCompletionSource?.TrySetResult(true) == true)
            {
                _startedAt = null;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                return true;
            }
            return false;
        }

        public void BeginWait(int duration, Action onTimeout)
        {
            if (_taskCompletionSource?.Task.IsCompleted == false)
            {
                throw new InvalidOperationException("TurnGate is already waiting.");
            }

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            

            _startedAt = DateTimeOffset.UtcNow;

            _ = WaitAsync(duration, onTimeout, _cancellationTokenSource.Token);
        }

        private async Task WaitAsync(int duration, Action onTimeout, CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(duration), token);
                if (_taskCompletionSource!.TrySetResult(false))
                {
                    _startedAt = null;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    onTimeout();
                }
            }
            catch (OperationCanceledException)
            {
                // User completed the action
            }
        }
    }
}
