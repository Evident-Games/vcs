using CommunityToolkit.Mvvm.ComponentModel;
using VCS.Content;
using VCS.Content.Disciplines;
using VCS.Application.Characters;
using VCS.Desktop.Sheet;
using VCS.Sheet;

namespace VCS.Desktop.Catalogs;

public partial class PowerItemViewModel : ObservableObject
{
    private readonly Func<string, int> _rating;
    private readonly Func<string> _clan;
    private readonly Func<string, string, int, DisciplineAdvancementGuidance> _advancement;
    public PowerItemViewModel(PowerDefinition definition, bool isKnown, Func<string, int> rating, bool unavailable = false,
        Func<string>? clan = null,
        Func<string, string, int, DisciplineAdvancementGuidance>? advancement = null)
    {
        Definition = definition;
        this.isKnown = isKnown;
        _rating = rating;
        _clan = clan ?? (() => "");
        _advancement = advancement ?? ((clanName, discipline, current) =>
            new CharacterSheetActions().DescribeDisciplineAdvancement(clanName, discipline, current));
        IsUnavailable = unavailable || DisciplineCatalog.UnverifiedPowerIds.Contains(definition.Id);
    }

    public PowerDefinition Definition { get; }
    public bool IsUnavailable { get; }
    [ObservableProperty] private bool isKnown;
    public string Heading => $"{Definition.Name} · Nível {Definition.Level}" +
        (Definition.AmalgamDiscipline is { } discipline ? $" · Amálgama: {discipline} {Definition.AmalgamLevel}" : "") +
        ($" · {SourceAttribution.Format(Definition.Source)}");
    public string Warning => DisciplineCatalog.UnverifiedPowerIds.Contains(Definition.Id)
        ? "Fonte não verificada: esta entrada não consta nos rituais do livro base fornecido. O registro foi preservado; resolva manualmente com o Narrador."
        : IsUnavailable
        ? "Entrada fora do catalogo atual. A selecao foi preservada; os detalhes originais nao estao disponiveis."
        : IsKnown && Definition.Level > _rating(Definition.Discipline)
            ? $"Aviso: requer nivel {Definition.Level} de {Definition.Discipline}; sua ficha tem {_rating(Definition.Discipline)}. Selecao livre."
            : IsKnown && Definition.AmalgamDiscipline is { } amalgam && _rating(amalgam) < Definition.AmalgamLevel
                ? $"Aviso: amalgama requer {amalgam} {Definition.AmalgamLevel}; sua ficha tem {_rating(amalgam)}. Selecao livre."
                : "";
    public string AdvancementInfo => IsUnavailable ? "Disciplina de origem nao identificada; custo nao calculado."
        : Definition.IsRitual ? "Ritual: os multiplicadores de disciplinas nao calculam o custo deste ritual."
        : DisciplineXpText.Describe(_advancement(_clan(), Definition.Discipline, _rating(Definition.Discipline)));
    public void RefreshWarning()
    {
        OnPropertyChanged(nameof(Warning));
        OnPropertyChanged(nameof(AdvancementInfo));
    }
    public TraitValue ToValue() => new(TraitCategory.DisciplinePower, Definition.Id, Definition.Name, Definition.Level);
}
