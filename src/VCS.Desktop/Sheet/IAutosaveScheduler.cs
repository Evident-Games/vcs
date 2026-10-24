using System.Windows.Threading;

namespace VCS.Desktop.Sheet;

public interface IAutosaveScheduler : IDisposable
{
    void Schedule(Func<Task> save);
    void Cancel();
}

public sealed class DispatcherAutosaveScheduler : IAutosaveScheduler
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private Func<Task>? _save;

    public DispatcherAutosaveScheduler() => _timer.Tick += OnTick;

    public void Schedule(Func<Task> save)
    {
        _timer.Stop();
        _save = save;
        _timer.Start();
    }

    public void Cancel() => _timer.Stop();

    private async void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_save is { } save) await save();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        _save = null;
    }
}
