namespace VCS.Desktop.Shell;

public interface IDesktopDialogs
{
    string? OpenJson();
    string? SaveJson();
    string? OpenBackup();
    string? SaveBackup();
    string? SavePdf() => throw new NotSupportedException("Exportacao PDF nao disponivel neste dialogo.");
    bool Confirm(string message);
}
