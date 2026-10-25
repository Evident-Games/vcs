using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Application.Catalogs;
using VCS.Desktop.Sheet;
using VCS.Domain.Characters;

namespace VCS.Desktop.Catalogs;

public partial class PowerItemViewModel : ObservableObject
{
    private readonly Func<string, int> _rating;
    private readonly Func<string> _clan;
    public PowerItemViewModel(PowerDefinition definition, bool isKnown, Func<string, int> rating, bool unavailable = false,
        Func<string>? clan = null)
    {
        Definition = definition;
        this.isKnown = isKnown;
        _rating = rating;
        _clan = clan ?? (() => "");
        IsUnavailable = unavailable;
    }

    public PowerDefinition Definition { get; }
    public bool IsUnavailable { get; }
    [ObservableProperty] private bool isKnown;
    public string Heading => $"{Definition.Name} · Nível {Definition.Level}" +
        (Definition.AmalgamDiscipline is { } discipline ? $" · Amalgama: {discipline} {Definition.AmalgamLevel}" : "");
    public string Warning => IsUnavailable
        ? "Entrada fora do catalogo atual. A selecao foi preservada; os detalhes originais nao estao disponiveis."
        : IsKnown && Definition.Level > _rating(Definition.RequiredDiscipline)
            ? $"Aviso: requer nivel {Definition.Level} de {Definition.RequiredDiscipline}; sua ficha tem {_rating(Definition.RequiredDiscipline)}. Selecao livre."
            : IsKnown && Definition.AmalgamDiscipline is { } amalgam && _rating(amalgam) < Definition.AmalgamLevel
                ? $"Aviso: amalgama requer {amalgam} {Definition.AmalgamLevel}; sua ficha tem {_rating(amalgam)}. Selecao livre."
                : "";
    public string AdvancementInfo => IsUnavailable ? "Disciplina de origem nao identificada; custo nao calculado."
        : Definition.IsRitual ? "Ritual: os multiplicadores de disciplinas nao calculam o custo deste ritual."
        : DisciplineXpText.Describe(_clan(), Definition.RequiredDiscipline, _rating(Definition.RequiredDiscipline));
    public void RefreshWarning()
    {
        OnPropertyChanged(nameof(Warning));
        OnPropertyChanged(nameof(AdvancementInfo));
    }
    public TraitValue ToValue() => new(TraitCategory.DisciplinePower, Definition.Id, Definition.Name, Definition.Level);
}
