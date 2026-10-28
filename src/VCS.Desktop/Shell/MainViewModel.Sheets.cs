using CommunityToolkit.Mvvm.Input;
using VCS.Application.Characters;
using VCS.Content.Clans;
using VCS.Desktop.Sheet;

namespace VCS.Desktop.Shell;

/// <summary>Open sheets: create, open, switch, close and sheet themes.</summary>
public partial class MainViewModel
{
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

    private void NotifyLayout()
    {
        OnPropertyChanged(nameof(IsSideBySide));
        OnPropertyChanged(nameof(LayoutColumns));
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
        next.ConfigureDisplay(Display);
        next.SheetThemeId = Display.SheetThemeFor(character.Id);
        next.KnownClans = ClanCatalog.All.Select(x => x.Name)
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
}
