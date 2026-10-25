namespace VCS.Desktop.Sheet;

public interface IAutosaveScheduler : IDisposable
{
    void Schedule(Func<Task> save);
    void Cancel();
}
