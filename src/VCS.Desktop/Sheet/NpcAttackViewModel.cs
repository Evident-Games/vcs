using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Sheet;

namespace VCS.Desktop.Sheet;

public partial class NpcAttackViewModel(NpcAttack attack) : ObservableObject
{
    [ObservableProperty] private string name = attack.Name;
    [ObservableProperty] private string dicePool = attack.DicePool;
    [ObservableProperty] private string damageBonus = attack.DamageBonus;
    [ObservableProperty] private string damageType = attack.DamageType;
    [ObservableProperty] private string activeDisciplines = attack.ActiveDisciplines;
    [ObservableProperty] private string notes = attack.Notes;

    public NpcAttack ToValue() => new(Name, DicePool, DamageBonus, DamageType, ActiveDisciplines, Notes);
}