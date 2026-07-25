namespace BelotWebApp.BelotClasses
{
    public class BelotTurnGate
    {
        private TaskCompletionSource<bool>? _taskCompletionSource;

        public bool Signal()
        {
            return _taskCompletionSource?.TrySetResult(true) ?? true;
        }

        public void BeginWait(int duration, Action onTimeout)
        {
            if (_taskCompletionSource?.Task.IsCompleted == false)
            {
                throw new InvalidOperationException("TurnGate is already waiting.");
            }

            _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = WaitAsync(duration, onTimeout);
        }

        private async Task WaitAsync(int duration, Action onTimeout)
        {
            var timeout = Task.Delay(duration * 1000).ContinueWith(_ => _taskCompletionSource!.TrySetResult(false));

            bool userCompletedTask = await _taskCompletionSource!.Task;

            if (!userCompletedTask)
            {
                onTimeout();
            }
        }
    }
}
