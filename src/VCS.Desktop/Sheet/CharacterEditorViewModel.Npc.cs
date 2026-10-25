using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Catalogs;
using VCS.Domain.Blood;
using VCS.Domain.Characters;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    private int _autoBloodPotency;
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty] private bool isActive;
    public string TabTitle => string.IsNullOrWhiteSpace(Name) ? (IsNpc ? "NPC sem nome" : "Sem nome") : Name;
    partial void OnNameChanged(string value) => OnPropertyChanged(nameof(TabTitle));
    public ObservableCollection<NpcAttackViewModel> NpcAttacks { get; } = [];
    public SheetFieldViewModel NpcBloodSurge => _extraFields["BloodSurge"];
    public SheetFieldViewModel NpcDamageMend => _extraFields["DamageMend"];
    public SheetFieldViewModel NpcPowerBonus => _extraFields["PowerBonus"];

    private void InitializeNpcSheet(CharacterDetails details)
    {
        foreach (var attack in details.NpcAttacks ?? []) AttachAttack(new(attack));
        if (!details.IsNpc) return;
        if (NpcAttacks.Count == 0) AttachAttack(new(new()));
        _autoBloodPotency = details.BloodPotency;
        FillBloodEffects(details.BloodPotency, onlyEmpty: true);
    }

    partial void OnBloodPotencyChanged(int oldValue, int newValue)
    {
        if (!_ready || !IsNpc) return;
        FillBloodEffects(newValue, onlyEmpty: false);
        _autoBloodPotency = newValue;
    }

    // Keeps values typed by the user; only empty fields or the previous automatic value follow Blood Potency.
    private void FillBloodEffects(int bloodPotency, bool onlyEmpty)
    {
        Update(NpcBloodSurge, BloodPotencyEffects.BloodSurge(_autoBloodPotency), BloodPotencyEffects.BloodSurge(bloodPotency));
        Update(NpcDamageMend, BloodPotencyEffects.DamageMend(_autoBloodPotency), BloodPotencyEffects.DamageMend(bloodPotency));
        Update(NpcPowerBonus, BloodPotencyEffects.PowerBonus(_autoBloodPotency), BloodPotencyEffects.PowerBonus(bloodPotency));

        void Update(SheetFieldViewModel field, string previous, string next)
        {
            if (string.IsNullOrWhiteSpace(field.Value) || (!onlyEmpty && field.Value == previous)) field.Value = next;
        }
    }

    [RelayCommand]
    private void AddNpcAttack()
    {
        AttachAttack(new(new()));
        MarkChanged();
    }

    [RelayCommand]
    private void RemoveNpcAttack(NpcAttackViewModel? attack)
    {
        if (attack is null || !NpcAttacks.Remove(attack)) return;
        attack.PropertyChanged -= OnNpcAttackChanged;
        MarkChanged();
    }

    private void AttachAttack(NpcAttackViewModel attack)
    {
        attack.PropertyChanged += OnNpcAttackChanged;
        NpcAttacks.Add(attack);
    }

    private void OnNpcAttackChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_ready) MarkChanged();
    }

    [RelayCommand] private void AddNpcHealthBox() => NpcHealthText = (NpcHealthCapacity + 1).ToString();
    [RelayCommand] private void RemoveNpcHealthBox() => NpcHealthText = (NpcHealthCapacity - 1).ToString();
    [RelayCommand] private void AddNpcWillpowerBox() => NpcWillpowerText = (NpcWillpowerCapacity + 1).ToString();
    [RelayCommand] private void RemoveNpcWillpowerBox() => NpcWillpowerText = (NpcWillpowerCapacity - 1).ToString();

    // Typed box counts: invalid text is ignored and values never drop below the damage already marked.
    public string NpcHealthText
    {
        get => NpcHealthCapacity.ToString();
        set
        {
            if (int.TryParse(value, out var boxes))
                NpcHealthCapacity = Math.Clamp(boxes, Math.Max(1, HealthSuperficial + HealthAggravated), CharacterRules.NpcMaximumBoxes);
            OnPropertyChanged();
        }
    }

    public string NpcWillpowerText
    {
        get => NpcWillpowerCapacity.ToString();
        set
        {
            if (int.TryParse(value, out var boxes))
                NpcWillpowerCapacity = Math.Clamp(boxes, Math.Max(1, WillpowerSuperficial + WillpowerAggravated), CharacterRules.NpcMaximumBoxes);
            OnPropertyChanged();
        }
    }


    internal void RemoveNpcDiscipline(NpcDisciplineSlotViewModel slot)
    {
        if (!NpcDisciplines.Remove(slot)) return;
        slot.Dispose();
    }

    private void DetachNpcAttacks()
    {
        foreach (var attack in NpcAttacks) attack.PropertyChanged -= OnNpcAttackChanged;
        foreach (var slot in NpcDisciplines) slot.Dispose();
    }

    public const int NpcDisciplineSlots = 3;
    /// <summary>The NPC sheet starts with 3 advantage and 3 flaw lines, in the player-sheet layout.</summary>
    public const int NpcMeritSlotsPerKind = 3;
    public ObservableCollection<NpcDisciplineSlotViewModel> NpcDisciplines { get; } = [];

    public SheetFieldViewModel NpcTrueAge => _extraFields["TrueAge"];
    public SheetFieldViewModel NpcApparentAge => _extraFields["ApparentAge"];
    public SheetFieldViewModel NpcHistory => _extraFields["History"];
    public SheetFieldViewModel NpcAppearance => _extraFields["Appearance"];
    [RelayCommand] private void AddNpcDiscipline() => NpcDisciplines.Add(new(this));

    // Rated disciplines get a line; empty lines pad the fixed NPC layout.
    private void RebuildNpcSlots()
    {
        foreach (var slot in NpcDisciplines) slot.Dispose();
        NpcDisciplines.Clear();
        foreach (var item in Disciplines.Items.Where(x => x.Rating > 0 || !IsCatalogDiscipline(x.Name))
                     .OrderByDescending(x => x.Rating).ThenBy(x => x.Name, StringComparer.CurrentCulture))
            NpcDisciplines.Add(new(this, item));
        while (NpcDisciplines.Count < NpcDisciplineSlots) NpcDisciplines.Add(new(this));
    }

    private bool IsCatalogDiscipline(string name) => Powers.AllPowers.Any(x => !x.Definition.IsRitual &&
        CatalogText.Normalize(x.Definition.Discipline) == CatalogText.Normalize(name));
}