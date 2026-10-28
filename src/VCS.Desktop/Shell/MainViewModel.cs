using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Application.Catalogs;
using VCS.Application.Characters;
using VCS.Application.Maintenance;
using VCS.Desktop.Database;
using VCS.Desktop.Layout;
using VCS.Desktop.Sheet;
using VCS.Desktop.Themes;

namespace VCS.Desktop.Shell;

public partial class MainViewModel(
    CharacterService service, ICharacterFiles files, IDatabaseMaintenance maintenance,
    IDesktopDialogs dialogs, ThemeService themes, Func<CharacterDto, CharacterEditorViewModel> createEditor,
    ILogger<MainViewModel> logger, DisplaySettingsStore? preferencesStore = null,
    IAdvantageCatalog? advantageCatalog = null, ISkillCatalog? skillCatalog = null,
    ICharacterPdfExporter? pdfExporter = null,
    UpdateService? updates = null, DatabaseEditorViewModel? database = null) : ObservableObject
{
    private IReadOnlyList<CharacterSummary> _all = [];

    [ObservableProperty] private CharacterEditorViewModel? editor;

    [ObservableProperty] private CharacterSummary? selectedCharacter;

    [ObservableProperty] private string searchText = "";

    [ObservableProperty] private bool isBusy;

    [ObservableProperty] private bool isLayoutSettingsOpen;

    [ObservableProperty] private bool isHelpOpen;
    [ObservableProperty] private bool isDatabaseEditorOpen;
    public DatabaseEditorViewModel? Database => database;
    public bool CanEditDatabase => database is not null;

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

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsIdle));

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
    private void ToggleLayoutSettings()
    {
        IsHelpOpen = false;
        IsDatabaseEditorOpen = false;
        if (!IsLayoutSettingsOpen) Display.IsEditingNpc = Editor?.IsNpc ?? false;
        IsLayoutSettingsOpen = !IsLayoutSettingsOpen;
    }

    [RelayCommand]
    private void ToggleHelp()
    {
        IsLayoutSettingsOpen = false;
        IsDatabaseEditorOpen = false;
        IsHelpOpen = !IsHelpOpen;
    }

    [RelayCommand]
    private Task ToggleDatabaseEditorAsync() => RunAsync(async () =>
    {
        if (database is null) return;
        IsLayoutSettingsOpen = false;
        IsHelpOpen = false;
        if (!IsDatabaseEditorOpen) await database.LoadAsync();
        IsDatabaseEditorOpen = !IsDatabaseEditorOpen;
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
