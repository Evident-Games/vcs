using Microsoft.Win32;
using System.Windows;

namespace VCS.Desktop.Services;

public interface IDesktopDialogs
{
    string? OpenJson();
    string? SaveJson();
    string? OpenBackup();
    string? SaveBackup();
    string? SavePdf() => throw new NotSupportedException("Exportacao PDF nao disponivel neste dialogo.");
    bool Confirm(string message);
}

public sealed class DesktopDialogs : IDesktopDialogs
{
    public string? OpenJson() => Open("Ficha VCS (*.json)|*.json");
    public string? SaveJson() => Save("Ficha VCS (*.json)|*.json", ".json", "personagem.json");
    public string? SavePdf() => Save("PDF preenchivel (*.pdf)|*.pdf", ".pdf", "ficha-vcs.pdf");
    public string? OpenBackup() => Open("Backup SQLite (*.db;*.bak)|*.db;*.bak|Todos os arquivos|*.*");
    public string? SaveBackup() => Save("Backup SQLite (*.db)|*.db", ".db", $"vcs-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");

    public bool Confirm(string message) => MessageBox.Show(
        System.Windows.Application.Current.MainWindow, message, "VCS", MessageBoxButton.YesNo,
        MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes;

    private static string? Open(string filter)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = true };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true ? dialog.FileName : null;
    }

    private static string? Save(string filter, string extension, string name)
    {
        var dialog = new SaveFileDialog { Filter = filter, DefaultExt = extension, FileName = name, AddExtension = true };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true ? dialog.FileName : null;
    }
}
