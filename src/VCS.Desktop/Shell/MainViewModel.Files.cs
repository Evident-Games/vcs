using CommunityToolkit.Mvvm.Input;
using VCS.Application.Characters;

namespace VCS.Desktop.Shell;

/// <summary>JSON import/export, PDF export, database backup and restore.</summary>
public partial class MainViewModel
{
    [RelayCommand]
    private Task ImportAsync() => RunAsync(async () =>
    {
        if (dialogs.OpenJson() is not { } path) return;
        var draft = await files.ReadAsync(path);
        var imported = await service.ImportAsync(draft);
        await RefreshAsync();
        await EditAsync(imported);
        Message = "Ficha importada como novo personagem.";
    });

    [RelayCommand]
    private Task ExportAsync() => RunAsync(async () =>
    {
        CharacterDto character;
        if (Editor is { } current)
        {
            if (!await current.FlushAsync())
            {
                Error = "Corrija ou salve a ficha antes de exportar.";
                return;
            }
            character = await service.GetAsync(current.Id);
        }
        else if (SelectedCharacter is { } selected) character = await service.GetAsync(selected.Id);
        else
        {
            Error = "Selecione um personagem para exportar.";
            return;
        }
        if (dialogs.SaveJson() is not { } path) return;
        await files.ExportAsync(character, path);
        Message = "Ficha exportada.";
    });

    [RelayCommand]
    private Task ExportPdfAsync() => RunAsync(async () =>
    {
        if (pdfExporter is null) throw new InvalidOperationException("O servico de PDF nao foi configurado.");
        if (Editor is { } current && !await current.FlushAsync())
        {
            Error = "Corrija ou salve a ficha antes de exportar.";
            return;
        }
        var id = Editor?.Id ?? SelectedCharacter?.Id;
        if (id is null) { Error = "Selecione uma ficha para exportar."; return; }
        var character = await service.GetAsync(id.Value);
        var sections = Display.Pc.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder)
            .Select(x => new PdfSection(x.Key, string.IsNullOrWhiteSpace(x.Caption) ? x.Title : x.Caption.Trim())).ToArray();
        if (character.Details.IsNpc) sections = [];
        else if (sections.Length == 0)
        {
            Error = "Ative ao menos uma secao em Exibir antes de exportar.";
            return;
        }
        if (dialogs.SavePdf() is not { } path) return;
        await pdfExporter.ExportAsync(character, path, sections);
        Message = "PDF preenchivel exportado. Edicoes no PDF nao sao importadas automaticamente para o VCS.";
    });

    [RelayCommand]
    private Task BackupAsync() => RunAsync(async () =>
    {
        if (Editor is { } current && !await current.FlushAsync())
        {
            Error = "Corrija ou salve a ficha antes de gerar um backup.";
            return;
        }
        if (dialogs.SaveBackup() is not { } path) return;
        await maintenance.BackupAsync(path);
        Message = $"Backup salvo: {path}";
    });

    [RelayCommand]
    private Task RestoreAsync() => RunAsync(async () =>
    {
        if (dialogs.OpenBackup() is not { } path ||
            !dialogs.Confirm("Restaurar este backup e substituir todas as fichas locais? Uma copia anterior sera preservada."))
            return;
        if (!await CloseAllEditorsAsync()) return;
        var safetyCopy = await maintenance.RestoreAsync(path);
        await RefreshAsync();
        Message = $"Backup restaurado. Copia anterior: {safetyCopy}";
    });
}
