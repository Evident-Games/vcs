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
    ICharacterPdfExporter? pdfExporter = null, IPredatorCatalog? predatorCatalog = null,
    UpdateService? updates = null) : ObservableObject
{
    private IReadOnlyList<CharacterSummary> _all = [];
    [ObservableProperty] private CharacterEditorViewModel? editor;
    [ObservableProperty] private CharacterSummary? selectedCharacter;
    [ObservableProperty] private string searchText = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isLayoutSettingsOpen;
    [ObservableProperty] private bool isHelpOpen;
    [ObservableProperty] private string error = "";
    [ObservableProperty] private UpdateInfo? availableUpdate;
    [ObservableProperty] private string message = "Modo provisorio: banco local criado automaticamente. Nenhuma configuracao necessaria.";
    public ObservableCollection<CharacterSummary> Characters { get; } = [];
    public ObservableCollection<CharacterEditorViewModel> OpenEditors { get; } = [];
    public bool HasOpenEditors => OpenEditors.Count != 0;
    public bool HasManyOpen => OpenEditors.Count > 1;
    public int SheetColumns => OpenEditors.Count switch { <= 2 => Math.Max(1, OpenEditors.Count), <= 4 => 2, _ => 3 };
    public bool IsSideBySide => Display.UseBento && HasManyOpen;
    public int LayoutColumns => IsSideBySide ? SheetColumns : 1;
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
    private Task _warmUp = Task.CompletedTask;

    partial void OnSelectedCharacterChanged(CharacterSummary? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanActOnCharacter));
    }

    partial void OnEditorChanged(CharacterEditorViewModel? oldValue, CharacterEditorViewModel? newValue)
    {
        if (oldValue is not null) oldValue.IsActive = false;
        if (newValue is not null) newValue.IsActive = true;
        if (newValue is not null) SelectedCharacter = Characters.FirstOrDefault(x => x.Id == newValue.Id) ?? SelectedCharacter;
        Display.ShowSheet(newValue?.Id);
        OnPropertyChanged(nameof(IsHome));
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(CanActOnCharacter));
    }

    private void OnCharacterSaved(CharacterDto saved)
    {
        var summary = new CharacterSummary(saved.Id, saved.Details.Name, saved.Details.Clan, saved.Details.Chronicle,
            saved.UpdatedUtc, saved.Revision, saved.Details.IsNpc);
        _all = _all.Select(x => x.Id == saved.Id ? summary : x).ToArray();
        // Autosave runs while typing: replace the one row instead of rebuilding the whole list.
        var index = Characters.Select(x => x.Id).ToList().IndexOf(saved.Id);
        if (index < 0 || !MatchesSearch(summary)) { ApplyFilter(); return; }
        var wasSelected = SelectedCharacter?.Id == saved.Id;
        Characters[index] = summary;
        if (wasSelected) SelectedCharacter = summary;
    }

    private bool MatchesSearch(CharacterSummary character) =>
        new[] { character.Name, character.Clan, character.Chronicle }
            .Any(x => x.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase));

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var selectedId = SelectedCharacter?.Id;
        Characters.Clear();
        foreach (var character in _all.Where(MatchesSearch))
            Characters.Add(character);
        SelectedCharacter = Characters.FirstOrDefault(x => x.Id == selectedId) ?? Characters.FirstOrDefault();
        OnPropertyChanged(nameof(HasCharacters));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CountLabel));
    }

    public Task InitializeAsync() => RunAsync(async () =>
    {
        await RefreshAsync();
        if (_warmUp.IsCompleted) _warmUp = WarmUpSaveAsync();
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

    private void NotifyLayout()
    {
        OnPropertyChanged(nameof(IsSideBySide));
        OnPropertyChanged(nameof(LayoutColumns));
    }

    private void OnDisplayChanged(object? sender, EventArgs e)
    {
        NotifyLayout();
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
        if (Editor is { } previous) await previous.FlushAsync();
        if (OpenEditors.FirstOrDefault(x => x.Id == character.Id) is { } open)
        {
            Editor = open;
            return;
        }
        var catalogEntries = advantageCatalog is null ? [] : await advantageCatalog.ListAsync();
        var next = createEditor(character);
        next.ConfigureAdvantageCatalog(catalogEntries);
        if (skillCatalog is not null) next.ConfigureSkillCatalog(await skillCatalog.ListAsync());
        if (predatorCatalog is not null) next.ConfigurePredatorCatalog(await predatorCatalog.ListAsync());
        next.ConfigureDisplay(Display);
        next.SheetThemeId = Display.SheetThemeFor(character.Id);
        next.KnownClans = VCS.Domain.Characters.ClanCatalog.All.Select(x => x.Name)
            .Concat(_all.Select(x => x.Clan)).Append(character.Details.Clan)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToArray();
        SelectedCharacter = Characters.FirstOrDefault(x => x.Id == character.Id);
        next.Saved += OnCharacterSaved;
        OpenEditors.Add(next);
        NotifyOpenEditors();
        Editor = next;
    }

    private void NotifyOpenEditors()
    {
        OnPropertyChanged(nameof(HasOpenEditors));
        OnPropertyChanged(nameof(HasManyOpen));
        OnPropertyChanged(nameof(SheetColumns));
        NotifyLayout();
    }

    private void CloseEditor(CharacterEditorViewModel editor)
    {
        var index = OpenEditors.IndexOf(editor);
        if (index < 0) return;
        editor.Saved -= OnCharacterSaved;
        OpenEditors.RemoveAt(index);
        if (ReferenceEquals(Editor, editor))
            Editor = OpenEditors.Count == 0 ? null : OpenEditors[Math.Min(index, OpenEditors.Count - 1)];
        editor.Dispose();
        NotifyOpenEditors();
    }

    private async Task<bool> CloseEditorAsync(CharacterEditorViewModel editor)
    {
        if (!await editor.FlushAsync() &&
            !dialogs.Confirm($"Nao foi possivel salvar '{editor.Name}'. Descartar as alteracoes pendentes e fechar?"))
            return false;
        CloseEditor(editor);
        return true;
    }

    [RelayCommand]
    private Task CloseTabAsync(CharacterEditorViewModel? editor) => RunAsync(async () =>
    {
        if ((editor ?? Editor) is { } target && await CloseEditorAsync(target)) await RefreshAsync();
    });

    /// <summary>Sets the active sheet theme; an empty id makes the sheet follow the application theme.</summary>
    [RelayCommand]
    private void SelectSheetTheme(string id)
    {
        if (Editor is null) return;
        Editor.SheetThemeId = id.Length == 0 ? null : id;
        Display.SetSheetTheme(Editor.Id, Editor.SheetThemeId);
    }

    [RelayCommand]
    private void ActivateEditor(CharacterEditorViewModel? editor)
    {
        if (editor is not null && OpenEditors.Contains(editor)) Editor = editor;
    }

    public async Task CheckForUpdateAsync()
    {
        if (updates is null) return;
        try { AvailableUpdate = await updates.CheckAsync(); }
        catch (Exception exception)
        {
            // Offline or GitHub unreachable: the app keeps working without notice.
            logger.LogWarning(exception, "Checagem de atualizacao falhou");
            return;
        }
        if (AvailableUpdate is not null)
            Message = $"Nova versao {AvailableUpdate.Version} disponivel (atual {UpdateService.CurrentVersion}). Clique em Atualizar.";
    }

    [RelayCommand]
    private Task UpdateAsync() => RunAsync(async () =>
    {
        if (updates is null || AvailableUpdate is null) return;
        Message = "Baixando atualizacao...";
        if (await updates.ApplyAsync(AvailableUpdate))
            System.Windows.Application.Current.Shutdown();
        else
        {
            UpdateService.OpenPage(AvailableUpdate.PageUrl);
            Message = "Sem permissao para atualizar aqui. Baixe a nova versao na pagina aberta.";
        }
    });

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
        var created = await service.CreateAsync();
        await RefreshAsync();
        await EditAsync(created);
    });

    [RelayCommand]
    private Task CreateNpcAsync() => RunAsync(async () =>
    {
        var created = await service.CreateNpcAsync();
        await RefreshAsync();
        await EditAsync(created);
    });

    [RelayCommand]
    private Task OpenAsync() => RunAsync(async () =>
    {
        if (Editor is not null)
        {
            await FlushOpenEditorsAsync();
            Editor = null;
            await RefreshAsync();
            Message = "Selecione uma ficha e use Arquivo > Abrir ficha, Enter ou duplo clique.";
            return;
        }
        await RefreshAsync();
        Message = "Selecione uma ficha na lista e abra com duplo clique ou Arquivo > Abrir ficha.";
        if (SelectedCharacter is { } selected) await EditAsync(await service.GetAsync(selected.Id));
    });

    [RelayCommand]
    private Task OpenCharacterAsync(CharacterSummary? character) => RunAsync(async () =>
    {
        if (character is null) { Error = "Selecione uma ficha para abrir."; return; }
        if (Editor?.Id == character.Id) return;
        await RefreshAsync();
        await EditAsync(await service.GetAsync(character.Id));
    });

    [RelayCommand]
    private void ToggleLayoutSettings()
    {
        IsHelpOpen = false;
        if (!IsLayoutSettingsOpen) Display.IsEditingNpc = Editor?.IsNpc ?? false;
        IsLayoutSettingsOpen = !IsLayoutSettingsOpen;
    }

    [RelayCommand]
    private void ToggleHelp()
    {
        IsLayoutSettingsOpen = false;
        IsHelpOpen = !IsHelpOpen;
    }

    [RelayCommand]
    private Task DuplicateAsync(CharacterSummary? character = null) => RunAsync(async () =>
    {
        var id = character?.Id ?? Editor?.Id ?? SelectedCharacter?.Id;
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
    private Task DeleteAsync(CharacterSummary? character = null) => RunAsync(async () =>
    {
        var id = character?.Id ?? Editor?.Id ?? SelectedCharacter?.Id;
        if (id is null) { Error = "Selecione uma ficha para excluir."; return; }
        var details = await service.GetAsync(id.Value);
        var deletesOpenEditor = OpenEditors.Any(x => x.Id == id);
        var warning = deletesOpenEditor ? " Incluindo qualquer rascunho aberto." : "";
        if (dialogs.Confirm($"Excluir '{details.Details.Name}' permanentemente?{warning}"))
        {
            await service.DeleteAsync(new(details.Id, details.Details.Name, details.Details.Clan,
                details.Details.Chronicle, details.UpdatedUtc, details.Revision));
            if (OpenEditors.FirstOrDefault(x => x.Id == id) is { } open) CloseEditor(open);
            await RefreshAsync();
            Message = "Personagem excluido.";
        }
    });

    private async Task<bool> CloseAllEditorsAsync()
    {
        foreach (var open in OpenEditors.ToArray())
            if (!await CloseEditorAsync(open)) return false;
        return true;
    }

    private async Task FlushOpenEditorsAsync()
    {
        foreach (var open in OpenEditors.ToArray()) await open.FlushAsync();
    }

    // Returns to the character list while keeping the open sheets available as tabs.
    [RelayCommand]
    private Task BackAsync() => RunAsync(async () =>
    {
        if (Editor is { } current && !await current.FlushAsync())
        {
            Error = "Corrija a ficha atual antes de voltar para a lista. Ela continua aberta na aba.";
            return;
        }
        await FlushOpenEditorsAsync();
        Editor = null;
        await RefreshAsync();
    });

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
        var sections = Display.Sections.Where(x => x.IsVisible).OrderBy(x => x.SectionOrder)
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

    public async Task<bool> TryCloseAsync()
    {
        if (IsBusy)
        {
            Error = "Aguarde a operacao em andamento antes de fechar.";
            return false;
        }
        await _warmUp;
        return await CloseAllEditorsAsync();
    }

    /// <summary>Background warm-up so the first autosave does not stall the UI; failures only cost that speed-up.</summary>
    private async Task WarmUpSaveAsync()
    {
        try { await Task.Run(() => service.WarmUpAsync()); }
        catch (Exception exception) { logger.LogDebug(exception, "Aquecimento do salvamento falhou"); }
    }
}
