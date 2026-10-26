using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VCS.Application.Characters;
using VCS.Content.Clans;
using VCS.Rules.V5.Validation;

namespace VCS.Desktop.Sheet;

/// <summary>Change tracking, draft snapshot, validation and autosave.</summary>
public partial class CharacterEditorViewModel
{
    private void MarkChanged()
    {
        RecalculateExperience();
        _changeVersion++;
        IsDirty = true;
        Error = "";
        Status = "Alteracoes nao salvas";
        ValidateDraft();
        RefreshCreation();
        _autosave.Schedule(async () => { await FlushAsync(); });
    }

    private CharacterDto Snapshot() => _current with
    {
        Details = new()
        {
            Advancement = _advancement,
            PredatorSelection = _predatorSelection, Stains = Stains,
            IsNpc = IsNpc, NpcPhysical = NpcPhysical, NpcSocial = NpcSocial, NpcMental = NpcMental,
            NpcHealthCapacity = NpcHealthCapacity, NpcWillpowerCapacity = NpcWillpowerCapacity,
            Name = Name, MortalName = MortalName, Clan = Clan, ClanKey = ClanCatalog.Find(Clan)?.Id ?? "", Generation = Generation,
            Sire = Sire, Predator = Predator, Concept = Concept, Chronicle = Chronicle,
            Description = Description, Notes = Notes, Hunger = Hunger, Humanity = Humanity,
            BloodPotency = BloodPotency, HealthSuperficial = HealthSuperficial, HealthAggravated = HealthAggravated,
            WillpowerSuperficial = WillpowerSuperficial, WillpowerAggravated = WillpowerAggravated,
            Ambition = Extra("Ambition"), Desire = Extra("Desire"), Resonance = Extra("Resonance"),
            ChronicleTenets = Extra("ChronicleTenets"), Touchstones = Extra("Touchstones"), Convictions = Extra("Convictions"),
            ClanBane = Extra("ClanBane"), TrueAge = Extra("TrueAge"), ApparentAge = Extra("ApparentAge"),
            BirthDate = Extra("BirthDate"), DeathDate = Extra("DeathDate"), Appearance = Extra("Appearance"),
            DistinguishingFeatures = Extra("DistinguishingFeatures"), History = Extra("History"),
            BloodSurge = Extra("BloodSurge"), DamageMend = Extra("DamageMend"), PowerBonus = Extra("PowerBonus"),
            RouseReroll = Extra("RouseReroll"), FeedingPenalty = Extra("FeedingPenalty"), BaneSeverity = Extra("BaneSeverity"),
            ExperienceTotal = ExperienceTotal, ExperienceSpent = ExperienceSpent,
            CampaignExperienceTotal = CampaignExperienceTotal, CampaignExperienceSpent = CampaignExperienceSpent,
            NpcAttacks = [.. NpcAttacks.Select(x => x.ToValue())],
            NoteEntries = Notebook.ToValues()
        },
        Traits = [.. Groups.SelectMany(x => x.Items).Select(x => x.ToValue()), .. Powers.SelectedValues()]
    };
    private string Extra(string key) => _extraFields[key].Value;

    private void ValidateDraft()
    {
        var snapshot = Snapshot();
        var previousFields = _issues.Select(x => x.Field).ToArray();
        _issues = CharacterRules.Validate(snapshot.Details, snapshot.Traits);
        foreach (var field in previousFields.Concat(_issues.Select(x => x.Field)).Distinct())
            ErrorsChanged?.Invoke(this, new(field));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ValidationSummary));
    }

    [RelayCommand]
    private async Task SaveAsync() => await FlushAsync();

    public async Task<bool> FlushAsync()
    {
        _autosave.Cancel();
        await _saveGate.WaitAsync();
        try
        {
            if (!IsDirty) return true;
            ValidateDraft();
            if (HasErrors)
            {
                Status = "Corrija a validacao para salvar";
                return false;
            }
            IsSaving = true;
            Error = "";
            Status = "Salvando...";
            var version = _changeVersion;
            var saved = await _service.SaveAsync(Snapshot());
            _current = saved;
            Saved?.Invoke(saved);
            OnPropertyChanged(nameof(UpdatedUtc));
            IsDirty = version != _changeVersion;
            Status = IsDirty ? "Alteracoes nao salvas" : $"Salvo as {DateTime.Now:HH:mm:ss}";
            if (IsDirty && !_disposed) _autosave.Schedule(async () => { await FlushAsync(); });
            return !IsDirty;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Falha ao salvar personagem {CharacterId}", Id);
            Error = exception.Message;
            Status = "Falha ao salvar; alteracoes continuam pendentes";
            return false;
        }
        finally
        {
            IsSaving = false;
            _saveGate.Release();
        }
    }
}
