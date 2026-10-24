namespace VCS.Application.Maintenance;

public interface IDatabaseMaintenance
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task BackupAsync(string path, CancellationToken cancellationToken = default);
    Task<string> RestoreAsync(string path, CancellationToken cancellationToken = default);
}
