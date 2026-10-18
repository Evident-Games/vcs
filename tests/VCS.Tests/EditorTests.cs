using Microsoft.Extensions.Logging.Abstractions;
using VCS.Application.Abstractions;
using VCS.Application.Characters;
using VCS.Desktop.Services;
using VCS.Desktop.ViewModels;
using VCS.Domain.Characters;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;

namespace VCS.Tests;

public sealed class EditorTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "VCS-editor-" + Guid.NewGuid().ToString("N"));
    private CharacterService _service = null!;
    private DelayedRepository _repository = null!;
    private readonly ManualAutosave _autosave = new();

    public async Task InitializeAsync()
    {
        var database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        await new SqliteDatabaseMaintenance(database).InitializeAsync();
        _repository = new(new SqliteCharacterRepository(database));
        _service = new(_repository);
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }

    private async Task<CharacterEditorViewModel> CreateEditorAsync() => new(
        await _service.CreateAsync(), _service, NullLogger<CharacterEditorViewModel>.Instance, _autosave,
        new ProvidedDisciplineCatalog());

    [Fact]
    public async Task Autosave_coalesces_edits_and_persists_latest_snapshot()
    {
        using var editor = await CreateEditorAsync();
        editor.Name = "Primeira edicao";
        editor.Name = "Ultima edicao";
        editor.Notes = "Notas";
        Assert.True(editor.IsDirty);
        Assert.Equal(3, _autosave.ScheduleCount);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal("Ultima edicao", saved.Details.Name);
        Assert.Equal("Notas", saved.Details.Notes);
        Assert.Equal(2, saved.Revision);
    }

    [Fact]
    public async Task Invalid_fields_do_not_overwrite_saved_data()
    {
        using var editor = await CreateEditorAsync();
        editor.Name = "";
        Assert.True(editor.HasErrors);
        Assert.NotEmpty(editor.GetErrors(nameof(editor.Name)).Cast<string>());
        Assert.False(await editor.FlushAsync());
        Assert.True(editor.IsDirty);
        Assert.Equal("Novo personagem", (await _service.GetAsync(editor.Id)).Details.Name);
        editor.Name = "Corrigido";
        Assert.False(editor.HasErrors);
        Assert.True(await editor.FlushAsync());
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task New_edit_during_save_remains_dirty_until_next_save()
    {
        using var editor = await CreateEditorAsync();
        editor.Name = "Primeiro";
        _repository.BlockNextSave();
        var saving = editor.FlushAsync();
        await _repository.SaveEntered.Task;
        editor.Name = "Segundo";
        _repository.ContinueSave.SetResult();
        Assert.False(await saving);
        Assert.True(editor.IsDirty);
        Assert.Equal("Primeiro", (await _service.GetAsync(editor.Id)).Details.Name);
        Assert.True(await editor.FlushAsync());
        Assert.Equal("Segundo", (await _service.GetAsync(editor.Id)).Details.Name);
    }

    [Fact]
    public async Task Storage_error_is_visible_and_retry_preserves_draft()
    {
        using var editor = await CreateEditorAsync();
        editor.Name = "Pendente";
        _repository.FailNextSave = true;
        Assert.False(await editor.FlushAsync());
        Assert.True(editor.IsDirty);
        Assert.Contains("disco", editor.Error);
        Assert.Equal("Novo personagem", (await _service.GetAsync(editor.Id)).Details.Name);
        Assert.True(await editor.FlushAsync());
        Assert.Equal("", editor.Error);
    }

    [Fact]
    public async Task Attribute_and_editable_list_changes_are_saved_and_recalculate_tracks()
    {
        using var editor = await CreateEditorAsync();
        editor.Attributes.Items.Single(x => x.Key == "Stamina").Rating = 4;
        Assert.Equal(7, editor.HealthCapacity);
        editor.Skills.AddCommand.Execute(null);
        Assert.True(editor.HasErrors);
        var skill = Assert.Single(editor.Skills.Items);
        skill.Name = "Habilidade pessoal";
        skill.Rating = 3;
        Assert.False(editor.HasErrors);
        Assert.True(await editor.FlushAsync());
        Assert.Contains((await _service.GetAsync(editor.Id)).Traits, x => x.Name == "Habilidade pessoal" && x.Rating == 3);
        editor.Skills.RemoveCommand.Execute(skill);
        Assert.True(await editor.FlushAsync());
        Assert.DoesNotContain((await _service.GetAsync(editor.Id)).Traits, x => x.Category == TraitCategory.Skill);
    }

    [Fact]
    public async Task Back_saves_before_showing_updated_list_and_search()
    {
        var model = CreateMain(new());
        await model.InitializeAsync();
        await model.CreateCommand.ExecuteAsync(null);
        Assert.True(model.IsEditing);
        model.Editor!.Name = "Nome de teste";
        model.Editor.Clan = "Cla pessoal";
        model.Editor.Chronicle = "Porto";
        await model.BackCommand.ExecuteAsync(null);
        Assert.True(model.IsHome);
        Assert.Equal("Nome de teste", Assert.Single(model.Characters).Name);
        model.SearchText = "PESSOAL";
        Assert.Single(model.Characters);
        model.SearchText = "ausente";
        Assert.True(model.IsEmpty);
        model.SearchText = "PORTO";
        Assert.Single(model.Characters);
        await model.DuplicateCommand.ExecuteAsync(null);
        Assert.EndsWith("(copia)", model.Editor!.Name);
        await model.BackCommand.ExecuteAsync(null);
        Assert.Equal(2, model.Characters.Count);
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Invalid_draft_blocks_navigation_and_close_until_user_discards()
    {
        var dialogs = new TestDialogs();
        var model = CreateMain(dialogs);
        await model.CreateCommand.ExecuteAsync(null);
        var id = model.Editor!.Id;
        model.Editor.Name = "";
        await model.BackCommand.ExecuteAsync(null);
        Assert.True(model.IsEditing);
        Assert.False(await model.TryCloseAsync());
        Assert.True(model.Editor.IsDirty);
        dialogs.ConfirmResult = true;
        Assert.True(await model.TryCloseAsync());
        Assert.True(model.IsHome);
        Assert.Equal("Novo personagem", (await _service.GetAsync(id)).Details.Name);
    }

    [Fact]
    public async Task Delete_requires_confirmation()
    {
        var dialogs = new TestDialogs();
        var model = CreateMain(dialogs);
        await model.CreateCommand.ExecuteAsync(null);
        await model.BackCommand.ExecuteAsync(null);
        await model.DeleteCommand.ExecuteAsync(null);
        Assert.Single(model.Characters);
        dialogs.ConfirmResult = true;
        await model.DeleteCommand.ExecuteAsync(null);
        Assert.True(model.IsEmpty);
        Assert.Empty(await _service.ListAsync());
    }

    private MainViewModel CreateMain(TestDialogs dialogs)
    {
        var database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        return new(_service, new JsonCharacterFiles(), new SqliteDatabaseMaintenance(database), dialogs,
            new ThemeService(), dto => new(dto, _service, NullLogger<CharacterEditorViewModel>.Instance, new ManualAutosave(),
                new ProvidedDisciplineCatalog()),
            NullLogger<MainViewModel>.Instance);
    }

    [Fact]
    public async Task Power_selection_autosaves_reopens_and_removal_is_persisted()
    {
        using var editor = await CreateEditorAsync();
        var power = editor.Powers.AllPowers.First(x => x.Definition.Level == 5 && !x.Definition.IsRitual);
        power.IsKnown = true;
        Assert.True(editor.IsDirty);
        Assert.False(editor.HasErrors);
        Assert.NotEmpty(power.Warning);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Contains(saved.Traits, x => x.Category == TraitCategory.DisciplinePower && x.Key == power.Definition.Id);
        using var reopened = new CharacterEditorViewModel(saved, _service, NullLogger<CharacterEditorViewModel>.Instance,
            new ManualAutosave(), new ProvidedDisciplineCatalog());
        var restored = reopened.Powers.AllPowers.Single(x => x.Definition.Id == power.Definition.Id);
        Assert.True(restored.IsKnown);
        Assert.NotEmpty(restored.Warning);
        restored.IsKnown = false;
        Assert.True(await reopened.FlushAsync());
        Assert.DoesNotContain((await _service.GetAsync(editor.Id)).Traits, x => x.Category == TraitCategory.DisciplinePower);
        Assert.Equal(11, reopened.Disciplines.Items.Count);
    }

    [Fact]
    public async Task Clan_selection_and_cost_updates_persist_without_granting_free_disciplines()
    {
        using var editor = await CreateEditorAsync();
        Assert.False(editor.IsDirty);
        Assert.Equal(14, editor.KnownClans.Length);
        editor.Clan = "Brujah";
        Assert.Contains("Celeridade", editor.ClanDisciplines);
        Assert.Equal(11, editor.Disciplines.Items.Count);
        Assert.All(editor.Disciplines.Items, x => Assert.Equal(0, x.Rating));
        var celerity = editor.Disciplines.Items.Single(x => x.Name == "Celeridade");
        celerity.Rating = 2;
        Assert.Contains("15 XP", celerity.AdvancementInfo);
        editor.Clan = "Tremere";
        Assert.Contains("21 XP", celerity.AdvancementInfo);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal("Tremere", saved.Details.Clan);
        using var reopened = new CharacterEditorViewModel(saved, _service, NullLogger<CharacterEditorViewModel>.Instance,
            new ManualAutosave(), new ProvidedDisciplineCatalog());
        Assert.False(reopened.IsDirty);
        var restored = reopened.Disciplines.Items.Single(x => x.Name == "Celeridade");
        Assert.Contains("21 XP", restored.AdvancementInfo);
        restored.Rating = 5;
        Assert.Contains("maximo", restored.AdvancementInfo);
    }

    [Fact]
    public async Task Sheet_attribute_groups_share_the_same_items_as_the_detailed_editor()
    {
        using var editor = await CreateEditorAsync();
        Assert.Equal(new[] { "Strength", "Dexterity", "Stamina" }, editor.PhysicalAttributes.Select(x => x.Key));
        Assert.Equal(new[] { "Charisma", "Manipulation", "Composure" }, editor.SocialAttributes.Select(x => x.Key));
        Assert.Equal(new[] { "Intelligence", "Wits", "Resolve" }, editor.MentalAttributes.Select(x => x.Key));
        var vigor = editor.PhysicalAttributes.Single(x => x.Key == "Stamina");
        Assert.Same(editor.Attributes.Items.Single(x => x.Key == "Stamina"), vigor);
        vigor.Rating = 4;
        editor.SocialAttributes.Single(x => x.Key == "Composure").Rating = 3;
        Assert.Equal(7, editor.HealthCapacity);
        Assert.Equal(4, editor.WillpowerCapacity);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        Assert.Equal(4, (await _service.GetAsync(editor.Id)).Traits.Single(x => x.Key == "Stamina").Rating);
    }

    [Fact]
    public async Task New_and_open_do_not_discard_an_invalid_draft_without_permission()
    {
        var model = CreateMain(new());
        await model.CreateCommand.ExecuteAsync(null);
        var id = model.Editor!.Id;
        model.Editor.Name = "";
        await model.CreateCommand.ExecuteAsync(null);
        Assert.Equal(id, model.Editor.Id);
        Assert.True(model.Editor.IsDirty);
        Assert.Single(await _service.ListAsync());
        await model.OpenCommand.ExecuteAsync(null);
        Assert.Equal(id, model.Editor.Id);
        Assert.True(model.IsEditing);
        model.Editor.Name = "Corrigido";
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Duplicate_and_exclude_target_the_open_editor_not_the_list_selection()
    {
        var dialogs = new TestDialogs { ConfirmResult = true };
        var model = CreateMain(dialogs);
        await model.CreateCommand.ExecuteAsync(null);
        var firstId = model.Editor!.Id;
        model.Editor.Name = "Primeiro";
        await model.CreateCommand.ExecuteAsync(null);
        var secondId = model.Editor!.Id;
        model.Editor.Name = "Segundo editado";
        model.SelectedCharacter = (await _service.ListAsync()).Single(x => x.Id == firstId);
        await model.DuplicateCommand.ExecuteAsync(null);
        Assert.Equal("Segundo editado (copia)", model.Editor!.Name);
        Assert.NotEqual(secondId, model.Editor.Id);
        await model.DeleteCommand.ExecuteAsync(null);
        Assert.True(model.IsHome);
        Assert.Equal(2, (await _service.ListAsync()).Count);
        Assert.Equal("Segundo editado", (await _service.GetAsync(secondId)).Details.Name);
    }

    [Fact]
    public async Task Extra_fields_and_experience_autosave_and_reopen()
    {
        var legacy = await _service.CreateAsync();
        legacy = legacy with { Details = legacy.Details with { Advancement = null } };
        using var editor = new CharacterEditorViewModel(legacy, _service, NullLogger<CharacterEditorViewModel>.Instance,
            _autosave, new ProvidedDisciplineCatalog());
        foreach (var field in editor.IdentityFields.Concat(editor.BackgroundFields).Concat(editor.BloodFields))
            field.Value = "Valor de " + field.Label;
        editor.ExperienceTotal = 50;
        editor.ExperienceSpent = 15;
        Assert.Equal(35, editor.ExperienceAvailable);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal("Valor de Ambicao", saved.Details.Ambition);
        Assert.Equal("Valor de Historia", saved.Details.History);
        Assert.Equal("Valor de Pilares", saved.Details.Touchstones);
        Assert.Equal("Valor de Rerrolagem de Sangue", saved.Details.RouseReroll);
        using var reopened = new CharacterEditorViewModel(saved, _service, NullLogger<CharacterEditorViewModel>.Instance,
            new ManualAutosave(), new ProvidedDisciplineCatalog());
        Assert.Equal(editor.IdentityFields.Select(x => x.Value), reopened.IdentityFields.Select(x => x.Value));
        Assert.Equal(editor.BackgroundFields.Select(x => x.Value), reopened.BackgroundFields.Select(x => x.Value));
        Assert.Equal(editor.BloodFields.Select(x => x.Value), reopened.BloodFields.Select(x => x.Value));
        Assert.Equal(35, reopened.ExperienceAvailable);
        reopened.ExperienceSpent = 51;
        Assert.True(reopened.HasErrors);
        Assert.False(await reopened.FlushAsync());
        Assert.Equal(15, (await _service.GetAsync(editor.Id)).Details.ExperienceSpent);
    }

    [Fact]
    public async Task Hiding_sections_never_removes_values_or_requires_saving()
    {
        using var editor = await CreateEditorAsync();
        editor.Name = "Persistido";
        editor.Attributes.Items[0].Rating = 4;
        Assert.True(await editor.FlushAsync());
        foreach (var section in editor.Display.Sections) section.IsVisible = false;
        Assert.Empty(editor.VisibleSections);
        Assert.True(editor.NoVisibleSections);
        Assert.False(editor.IsDirty);
        Assert.Null(editor.SelectedTab);
        editor.Display.UseTabs = true;
        editor.Display.Sections.Single(x => x.Key == "Attributes").IsVisible = true;
        Assert.Equal("Attributes", Assert.Single(editor.VisibleSections).Key);
        Assert.Equal("Attributes", Assert.Single(editor.SelectedTab!.Sections).Key);
        Assert.Equal(4, editor.PhysicalAttributes[0].Rating);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task Import_cancel_preserves_invalid_editor_and_existing_data()
    {
        var source = await _service.CreateAsync();
        var path = Path.Combine(_directory, "import.json");
        await new JsonCharacterFiles().ExportAsync(source, path);
        var model = CreateMain(new() { JsonToOpen = path });
        await model.CreateCommand.ExecuteAsync(null);
        var id = model.Editor!.Id;
        model.Editor.Name = "";
        await model.ImportCommand.ExecuteAsync(null);
        Assert.Equal(id, model.Editor.Id);
        Assert.True(model.Editor.IsDirty);
        Assert.Equal(2, (await _service.ListAsync()).Count);
        model.Editor.Name = "Corrigido";
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Different_menu_commands_cannot_run_during_a_pending_save()
    {
        var model = CreateMain(new());
        await model.CreateCommand.ExecuteAsync(null);
        model.Editor!.Name = "Edicao pendente";
        _repository.BlockNextSave();
        var duplicate = model.DuplicateCommand.ExecuteAsync(null);
        await _repository.SaveEntered.Task;
        await model.CreateCommand.ExecuteAsync(null);
        Assert.Contains("Aguarde", model.Error);
        Assert.Single(await _service.ListAsync());
        _repository.ContinueSave.SetResult();
        await duplicate;
        Assert.Equal("Edicao pendente (copia)", model.Editor!.Name);
        Assert.Equal(2, (await _service.ListAsync()).Count);
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Invalid_preferences_report_an_error_without_hiding_characters()
    {
        await _service.CreateAsync();
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(path, "{invalid");
        var maintenance = new SqliteDatabaseMaintenance(new DatabaseAccess(Path.Combine(_directory, "test.db")));
        var model = new MainViewModel(_service, new JsonCharacterFiles(), maintenance, new TestDialogs(),
            new ThemeService(), dto => new(dto, _service, NullLogger<CharacterEditorViewModel>.Instance,
                new ManualAutosave(), new ProvidedDisciplineCatalog()), NullLogger<MainViewModel>.Instance,
            new DisplaySettingsStore(path));
        await model.InitializeAsync();
        Assert.NotEmpty(model.Error);
        Assert.Single(model.Characters);
        await model.CreateCommand.ExecuteAsync(null);
        Assert.True(model.IsEditing);
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Opening_a_row_uses_its_id_instead_of_previous_list_selection()
    {
        var first = await _service.CreateAsync();
        var second = await _service.CreateAsync();
        var model = CreateMain(new());
        await model.InitializeAsync();
        model.SelectedCharacter = model.Characters.Single(x => x.Id == first.Id);
        await model.OpenCharacterCommand.ExecuteAsync(model.Characters.Single(x => x.Id == second.Id));
        Assert.Equal(second.Id, model.Editor!.Id);
        Assert.True(await model.TryCloseAsync());
    }

    [Fact]
    public async Task Sections_can_be_grouped_split_and_hidden_without_changing_character()
    {
        using var editor = await CreateEditorAsync();
        var firstTab = editor.VisibleTabs[0];
        Assert.Equal(new[] { "Identity", "Attributes" }, firstTab.Sections.Select(x => x.Key));
        Assert.Equal(7, editor.VisibleTabs.Count);
        editor.SelectedTab = firstTab;
        editor.Display.Sections.Single(x => x.Key == "State").TabName = " Identidade e atributos ";
        Assert.Equal(6, editor.VisibleTabs.Count);
        Assert.Equal(3, editor.VisibleTabs[0].Sections.Count);
        Assert.Equal(editor.VisibleTabs[0], editor.SelectedTab);
        editor.Display.Sections.Single(x => x.Key == "Attributes").TabName = "Atributos separados";
        Assert.Equal(7, editor.VisibleTabs.Count);
        editor.Display.Sections.Single(x => x.Key == "State").IsVisible = false;
        Assert.DoesNotContain(editor.VisibleTabs.SelectMany(x => x.Sections), x => x.Key == "State");
        foreach (var section in editor.Display.Sections) section.TabName = "Tudo";
        Assert.Single(editor.VisibleTabs);
        foreach (var section in editor.Display.Sections) section.IsVisible = false;
        Assert.Empty(editor.VisibleTabs);
        Assert.Null(editor.SelectedTab);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task Advantage_catalog_search_selection_costs_and_autosave_work()
    {
        var database = new DatabaseAccess(Path.Combine(_directory, "test.db"));
        using var editor = await CreateEditorAsync();
        editor.ConfigureAdvantageCatalog(await new SqliteAdvantageCatalog(database).ListAsync());
        var catalog = editor.AdvantageCatalog!;
        Assert.Equal(43, catalog.Entries.Count);
        Assert.False(editor.IsDirty);
        catalog.Search = "esofago";
        Assert.Equal("Esôfago de Ferro", Assert.Single(catalog.Entries).Name);
        Assert.Equal(3, catalog.Points);
        Assert.Equal(3, catalog.Minimum);
        Assert.Equal(3, catalog.Maximum);
        catalog.Points = 2;
        catalog.AddSelectedCommand.Execute(null);
        Assert.Empty(editor.Advantages.Items);
        Assert.Contains("3", catalog.Message);
        catalog.Points = 3;
        catalog.AddSelectedCommand.Execute(null);
        var iron = Assert.Single(editor.Advantages.Items);
        Assert.Equal(3, iron.Rating);
        Assert.Contains("sangue rançoso", iron.AdvancementInfo);
        catalog.AddSelectedCommand.Execute(null);
        Assert.Single(editor.Advantages.Items);
        Assert.Contains("ja esta", catalog.Message);
        catalog.Search = "inimigo";
        catalog.SelectedEntry = catalog.Entries.Single(x => x.Name == "Inimigo");
        catalog.Points = 5;
        catalog.AddSelectedCommand.Execute(null);
        Assert.Equal(5, Assert.Single(editor.Flaws.Items).Rating);
        await _autosave.FireAsync();
        Assert.False(editor.IsDirty);
        var saved = await _service.GetAsync(editor.Id);
        Assert.Contains(saved.Traits, x => x.Key == "catalog-esofago-de-ferro" && x.Rating == 3);
        Assert.Contains(saved.Traits, x => x.Key == "catalog-inimigo" && x.Rating == 5);
        Assert.Equal(1, saved.Traits.Single(x => x.Key == "Charisma").Rating);
        iron.Rating = 1;
        Assert.Contains("fora da referencia", iron.AdvancementInfo);
        editor.Advantages.RemoveCommand.Execute(iron);
        catalog.Search = "esofago";
        catalog.AddSelectedCommand.Execute(null);
        Assert.Single(editor.Advantages.Items);
        catalog.Search = "sem resultado xyz";
        Assert.Empty(catalog.Entries);
        Assert.False(catalog.HasSelection);
        catalog.AddSelectedCommand.Execute(null);
        Assert.Contains("Selecione", catalog.Message);
        Assert.True(await editor.FlushAsync());
    }

    [Fact]
    public async Task All_discipline_tracks_and_blood_potency_save_without_creating_a_ritual_discipline()
    {
        using var editor = await CreateEditorAsync();
        Assert.Equal(11, editor.Disciplines.Items.Count);
        Assert.DoesNotContain(editor.Disciplines.Items, x => x.Name == "Rituais");
        Assert.False(editor.IsDirty);
        var ritual = editor.Powers.Rituals.First();
        ritual.IsKnown = true;
        editor.Disciplines.Items.Single(x => x.Name == "Ofuscação").Rating = 5;
        editor.BloodPotency = 10;
        Assert.True(await editor.FlushAsync());
        var saved = await _service.GetAsync(editor.Id);
        Assert.Equal(10, saved.Details.BloodPotency);
        Assert.Contains(saved.Traits, x => x.Key == ritual.Definition.Id && x.Category == TraitCategory.DisciplinePower);
        Assert.DoesNotContain(saved.Traits, x => x.Category == TraitCategory.Discipline && x.Name == "Rituais");
        Assert.Equal(11, saved.Traits.Count(x => x.Category == TraitCategory.Discipline));
    }

    [Fact]
    public async Task Section_and_tab_order_and_titles_can_be_changed_independently()
    {
        using var editor = await CreateEditorAsync();
        var state = editor.Display.Sections.Single(x => x.Key == "State");
        state.Caption = "Estado de sangue";
        state.SectionOrder = 1;
        editor.Display.Sections.Single(x => x.Key == "Identity").SectionOrder = 10;
        editor.Display.Sections.Single(x => x.Key == "Attributes").SectionOrder = 11;
        state.TabName = "Sangue";
        state.TabOrder = 1;
        editor.Display.Sections.Single(x => x.Key == "Identity").TabOrder = 20;
        editor.Display.Sections.Single(x => x.Key == "Attributes").TabOrder = 20;
        Assert.Equal("State", editor.VisibleSections[0].Key);
        Assert.Equal("Estado de sangue", editor.VisibleSections[0].Title);
        Assert.Equal("Sangue", editor.VisibleTabs[0].Title);
        Assert.Equal("Identidade e atributos", editor.VisibleTabs[^1].Title);
        Assert.False(editor.IsDirty);
    }

    [Fact]
    public async Task Sidebar_summary_updates_after_save_and_invalid_draft_blocks_switch()
    {
        var other = await _service.CreateAsync();
        var model = CreateMain(new());
        await model.CreateCommand.ExecuteAsync(null);
        var id = model.Editor!.Id;
        model.Editor.Name = "Nome atualizado na lateral";
        model.Editor.Clan = "Brujah";
        Assert.True(await model.Editor.FlushAsync());
        Assert.Equal("Nome atualizado na lateral", model.Characters.Single(x => x.Id == id).Name);
        Assert.Equal("Brujah", model.Characters.Single(x => x.Id == id).Clan);
        model.Editor.Name = "";
        await model.OpenCharacterCommand.ExecuteAsync(model.Characters.Single(x => x.Id == other.Id));
        Assert.Equal(id, model.Editor.Id);
        Assert.True(model.Editor.IsDirty);
        model.Editor.Name = "Corrigido";
        Assert.True(await model.TryCloseAsync());
    }

    private sealed class TestDialogs : IDesktopDialogs
    {
        public bool ConfirmResult { get; set; }
        public string? JsonToOpen { get; init; }
        public bool Confirm(string message) => ConfirmResult;
        public string? OpenJson() => JsonToOpen;
        public string? SaveJson() => null;
        public string? OpenBackup() => null;
        public string? SaveBackup() => null;
    }

    private sealed class ManualAutosave : IAutosaveScheduler
    {
        private Func<Task>? _action;
        public int ScheduleCount { get; private set; }
        public void Schedule(Func<Task> save) { ScheduleCount++; _action = save; }
        public void Cancel() => _action = null;
        public async Task FireAsync()
        {
            var action = _action;
            _action = null;
            if (action is not null) await action();
        }
        public void Dispose() => Cancel();
    }

    private sealed class DelayedRepository(ICharacterRepository inner) : ICharacterRepository
    {
        private bool _block;
        public bool FailNextSave { get; set; }
        public TaskCompletionSource SaveEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ContinueSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void BlockNextSave() => _block = true;
        public Task<IReadOnlyList<Character>> ListAsync(CancellationToken cancellationToken = default) => inner.ListAsync(cancellationToken);
        public Task<Character?> GetAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetAsync(id, cancellationToken);
        public Task DeleteAsync(Guid id, long revision, CancellationToken cancellationToken = default) => inner.DeleteAsync(id, revision, cancellationToken);
        public async Task SaveAsync(Character character, long? revision, CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new IOException("Falha de disco simulada");
            }
            if (_block)
            {
                _block = false;
                SaveEntered.SetResult();
                await ContinueSave.Task;
            }
            await inner.SaveAsync(character, revision, cancellationToken);
        }
    }
}
