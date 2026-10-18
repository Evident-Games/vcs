using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Application.Abstractions;
using VCS.Application.Characters;
using VCS.Desktop.Services;

namespace VCS.Desktop.ViewModels;

public partial class MainViewModel(
    CharacterService service, ICharacterFiles files, IDatabaseMaintenance maintenance,
    IDesktopDialogs dialogs, ThemeService themes, Func<CharacterDto, CharacterEditorViewModel> createEditor,
    ILogger<MainViewModel> logger, DisplaySettingsStore? preferencesStore = null,
    IAdvantageCatalog? advantageCatalog = null, ISkillCatalog? skillCatalog = null,
    ICharacterPdfExporter? pdfExporter = null) : ObservableObject
{
    private IReadOnlyList<CharacterSummary> _all = [];
    [ObservableProperty] private CharacterEditorViewModel? editor;
    [ObservableProperty] private CharacterSummary? selectedCharacter;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLayoutSettingsOpen;
    [ObservableProperty] private bool isHelpOpen;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private string message = "Modo provisorio: banco local criado automaticamente. Nenhuma configuracao necessaria.";
    public ObservableCollection<CharacterSummary> Characters { get; } = [];
    public bool IsHome => Editor is null;
    public bool IsEditing => Editor is not null;
    public bool IsIdle => !IsBusy;
    public bool HasCharacters => Characters.Count != 0;
    public bool IsEmpty => !HasCharacters;
    public bool HasSelection => SelectedCharacter is not null;
    public string CountLabel => $"{Characters.Count} personagem(ns)";
    public DisplaySettingsViewModel Display { get; } = new();
    public bool CanActOnCharacter => Editor is not null || SelectedCharacter is not null;
    private bool _displayInitialized;

    partial void OnSelectedCharacterChanged(CharacterSummary? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanActOnCharacter));
    }

    partial void OnEditorChanged(CharacterEditorViewModel? oldValue, CharacterEditorViewModel? newValue)
    {
        if (oldValue is not null) oldValue.Saved -= OnCharacterSaved;
        if (newValue is not null) newValue.Saved += OnCharacterSaved;
        OnPropertyChanged(nameof(IsHome));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanActOnCharacter));
    }

    private void OnCharacterSaved(CharacterDto saved)
    {
        _all = _all.Select(x => x.Id == saved.Id
            ? new CharacterSummary(saved.Id, saved.Details.Name, saved.Details.Clan, saved.Details.Chronicle, saved.UpdatedUtc, saved.Revision, saved.Details.IsNpc)
            : x).ToArray();
        ApplyFilter();
    }

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var selectedId = SelectedCharacter?.Id;
        var term = SearchText.Trim();
        Characters.Clear();
        foreach (var character in _all.Where(c => new[] { c.Name, c.Clan, c.Chronicle }
                     .Any(x => x.Contains(term, StringComparison.OrdinalIgnoreCase))))
            Characters.Add(character);
        SelectedCharacter = Characters.FirstOrDefault(x => x.Id == selectedId) ?? Characters.FirstOrDefault();
        OnPropertyChanged(nameof(HasCharacters));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CountLabel));
    }

    public Task InitializeAsync() => RunAsync(async () =>
    {
        await RefreshAsync();
        if (!_displayInitialized)
        {
            try
            {
                preferencesStore?.Load(Display);
                if (preferencesStore is not null) themes.Apply(Display.ThemeId);
            }
            finally
            {
                Display.Changed += OnDisplayChanged;
                _displayInitialized = true;
            }
        }
    });

    private void OnDisplayChanged(object? sender, EventArgs e)
    {
        try
        {
            if (preferencesStore is not null)
            {
                themes.Apply(Display.ThemeId);
                preferencesStore.Save(Display);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao aplicar preferencias de exibicao");
            Error = $"Nao foi possivel salvar as preferencias: {exception.Message}";
        }
    }

    private async Task RefreshAsync()
    {
        _all = await service.ListAsync();
        ApplyFilter();
    }

    private async Task EditAsync(CharacterDto character)
    {
        var catalogEntries = advantageCatalog is null ? [] : await advantageCatalog.ListAsync();
        Editor?.Dispose();
        var next = createEditor(character);
        next.ConfigureAdvantageCatalog(catalogEntries);
        if (skillCatalog is not null) next.ConfigureSkillCatalog(await skillCatalog.ListAsync());
        next.ConfigureDisplay(Display);
        next.KnownClans = VCS.Domain.Characters.ClanCatalog.All.Select(x => x.Name)
            .Concat(_all.Select(x => x.Clan)).Append(character.Details.Clan)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        SelectedCharacter = Characters.FirstOrDefault(x => x.Id == character.Id);
        Editor = next;
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            Error = "Aguarde a operacao atual antes de iniciar outra.";
            return;
        }
        IsBusy = true;
        Error = "";
        try { await action(); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha na operacao desktop");
            Error = exception.Message;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task CreateAsync() => RunAsync(async () =>
    {
        if (!await LeaveEditorAsync()) return;
        var created = await service.CreateAsync();
        await RefreshAsync();
        await EditAsync(created);
    });

    [RelayCommand]
    private Task CreateNpcAsync() => RunAsync(async () =>
    {
        if (!await LeaveEditorAsync()) return;
        var created = await service.CreateNpcAsync();
        await RefreshAsync();
        await EditAsync(created);
    });

    [RelayCommand]
    private Task OpenAsync() => RunAsync(async () =>
    {
        if (Editor is not null)
        {
            if (await LeaveEditorAsync()) await RefreshAsync();
            Message = "Selecione uma ficha e use Arquivo > Abrir ficha, Enter ou duplo clique.";
            return;
        }
        if (!await LeaveEditorAsync()) return;
        await RefreshAsync();
        Message = "Selecione uma ficha na lista e abra com duplo clique ou Arquivo > Abrir ficha.";
        if (SelectedCharacter is { } selected) await EditAsync(await service.GetAsync(selected.Id));
    });

    [RelayCommand]
    private Task OpenCharacterAsync(CharacterSummary? character) => RunAsync(async () =>
    {
        if (character is null) { Error = "Selecione uma ficha para abrir."; return; }
        if (Editor?.Id == character.Id) return;
        if (!await LeaveEditorAsync()) return;
        await RefreshAsync();
        await EditAsync(await service.GetAsync(character.Id));
    });

    [RelayCommand]
    private void ToggleLayoutSettings()
    {
        IsHelpOpen = false;
        IsLayoutSettingsOpen = !IsLayoutSettingsOpen;
    }

    [RelayCommand]
    private void ToggleHelp()
    {
        IsLayoutSettingsOpen = false;
        IsHelpOpen = !IsHelpOpen;
    }

    [RelayCommand]
    private Task DuplicateAsync() => RunAsync(async () =>
    {
        var id = Editor?.Id ?? SelectedCharacter?.Id;
        if (id is null) { Error = "Selecione uma ficha para duplicar."; return; }
        if (Editor is { } current && !await current.FlushAsync())
        {
            Error = "Salve ou corrija a ficha antes de duplicar.";
            return;
        }
        var copy = await service.DuplicateAsync(id.Value);
        await RefreshAsync();
        await EditAsync(copy);
    });

    [RelayCommand]
    private Task DeleteAsync() => RunAsync(async () =>
    {
        var id = Editor?.Id ?? SelectedCharacter?.Id;
        if (id is null) { Error = "Selecione uma ficha para excluir."; return; }
        var character = await service.GetAsync(id.Value);
        if (dialogs.Confirm($"Excluir '{character.Details.Name}' permanentemente? Incluindo qualquer rascunho aberto."))
        {
            await service.DeleteAsync(new(character.Id, character.Details.Name, character.Details.Clan,
                character.Details.Chronicle, character.UpdatedUtc, character.Revision));
            Editor?.Dispose();
            Editor = null;
            await RefreshAsync();
            Message = "Personagem excluido.";
        }
    });

    private async Task<bool> LeaveEditorAsync()
    {
        if (Editor is not { } current) return true;
        if (!await current.FlushAsync() &&
            !dialogs.Confirm("Nao foi possivel salvar. Descartar as alteracoes pendentes e continuar?"))
            return false;
        current.Dispose();
        Editor = null;
        return true;
    }

    [RelayCommand]
    private Task BackAsync() => RunAsync(async () =>
    {
        if (await LeaveEditorAsync()) await RefreshAsync();
    });

    [RelayCommand]
    private Task ImportAsync() => RunAsync(async () =>
    {
        if (dialogs.OpenJson() is not { } path) return;
        var draft = await files.ReadAsync(path);
        if (!await LeaveEditorAsync()) return;
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
        var sections = Display.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder)
            .Select(x => new PdfSection(x.Key, string.IsNullOrWhiteSpace(x.Caption) ? x.Title : x.Caption.Trim())).ToArray();
        if (sections.Length == 0)
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
        if (!await LeaveEditorAsync()) return;
        var safetyCopy = await maintenance.RestoreAsync(path);
        await RefreshAsync();
        Message = $"Backup restaurado. Copia anterior: {safetyCopy}";
    });

    public async Task<bool> TryCloseAsync()
    {
        if (IsBusy)
        {
            Error = "Aguarde a operacao em andamento antes de fechar.";
            return false;
        }
        return await LeaveEditorAsync();
    }
}
