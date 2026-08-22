namespace TicketSystem.SharedUI.Services;

public sealed class LoadingState
{
    private int activeCount;

    public bool IsLoading => activeCount > 0;

    public event Action? Changed;

    public IDisposable BeginLoading()
    {
        if (Interlocked.Increment(ref activeCount) == 1)
        {
            Changed?.Invoke();
        }

        return new LoadingScope(this);
    }

    private void EndLoading()
    {
        if (Interlocked.Decrement(ref activeCount) == 0)
        {
            Changed?.Invoke();
        }
    }

    private sealed class LoadingScope : IDisposable
    {
        private readonly LoadingState state;
        private bool disposed;

        public LoadingScope(LoadingState state)
        {
            this.state = state;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            state.EndLoading();
        }
    }
}
