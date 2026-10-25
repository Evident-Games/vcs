using CommunityToolkit.Mvvm.Input;
using VCS.Application.Characters;

namespace VCS.Desktop.Shell;

/// <summary>Character list: search, refresh, duplicate and delete.</summary>
public partial class MainViewModel
{
    partial void OnSelectedCharacterChanged(CharacterSummary? value)
    {
        OnPropertyChanged(nameof(HasSelection));
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

    private async Task RefreshAsync()
    {
        _all = await service.ListAsync();
        ApplyFilter();
    }

    [RelayCommand]
    private Task OpenCharacterAsync(CharacterSummary? character) => RunAsync(async () =>
    {
        if (character is null) { Error = "Selecione uma ficha para abrir."; return; }
        if (Editor?.Id == character.Id) return;
        await RefreshAsync();
        await EditAsync(await service.GetAsync(character.Id));
    });

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
}
