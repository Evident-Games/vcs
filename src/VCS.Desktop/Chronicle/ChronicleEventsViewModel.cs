using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VCS.Application.Characters;
using VCS.Application.Chronicle;

namespace VCS.Desktop.Chronicle;

public partial class ChronicleEventsViewModel(IChronicleEventStore store) : ObservableObject
{
    [ObservableProperty] private int pool = 5;
    [ObservableProperty] private int rollHunger;
    [ObservableProperty] private int difficulty = 3;

    [RelayCommand]
    private void RollScene()
    {
        if (Pool is < 1 or > 50 || RollHunger is < 0 or > 5 || Difficulty is < 1 or > 50)
        {
            Status = "Use parada 1–50, Fome 0–5 e dificuldade 1–50.";
            return;
        }
        var result = new VCS.Application.Dice.DiceActions().Roll(Pool, RollHunger, Difficulty);
        var label = result.Outcome switch
        {
            VCS.Rules.V5.Dice.TestOutcome.MessyCritical => "crítico bestial",
            VCS.Rules.V5.Dice.TestOutcome.BestialFailure => "falha bestial",
            VCS.Rules.V5.Dice.TestOutcome.CriticalSuccess => "sucesso crítico",
            VCS.Rules.V5.Dice.TestOutcome.Success => "sucesso",
            _ => "falha"
        };
        Outcome += $"{Environment.NewLine}{Kind}: {label}; {result.Successes} sucessos / dificuldade {Difficulty}. " +
            $"Comuns [{string.Join(", ", result.NormalDice)}]; Fome [{string.Join(", ", result.HungerDice)}].";
        Status = "Rolagem registrada no rascunho. Salve o evento para manter o resultado.";
    }

    public IReadOnlyList<VCS.Rules.V5.Chronicle.HuntingApproach> HuntingApproaches => VCS.Rules.V5.Chronicle.HuntingRules.Approaches;
    [ObservableProperty] private VCS.Rules.V5.Chronicle.HuntingApproach selectedApproach = VCS.Rules.V5.Chronicle.HuntingRules.Approaches[0];
    [ObservableProperty] private int territoryDifficulty = 3;
    [ObservableProperty] private int securityModifier;
    [ObservableProperty] private int crowdModifier;
    [ObservableProperty] private int otherHuntModifier;
    [ObservableProperty] private int diableristHumanity = 7;
    [ObservableProperty] private int diableristGeneration = 13;
    [ObservableProperty] private int victimGeneration = 12;
    [ObservableProperty] private int diableristSuccesses;
    [ObservableProperty] private int victimSuccesses;
    [ObservableProperty] private bool soulConsumed;

    [RelayCommand]
    private void PrepareHunt()
    {
        try
        {
            Difficulty = VCS.Rules.V5.Chronicle.HuntingRules.Difficulty(TerritoryDifficulty, SecurityModifier, CrowdModifier, OtherHuntModifier);
            Kind = "Caça";
            if (string.IsNullOrWhiteSpace(Title)) Title = "Caçada";
            Notes += $"{Environment.NewLine}Abordagem: {SelectedApproach.Name}; parada sugerida: {SelectedApproach.Pool}. " +
                $"Dificuldade {Difficulty} (território {TerritoryDifficulty}, segurança {SecurityModifier}, aglomeração {CrowdModifier}, outros {OtherHuntModifier}).";
            Source = "Vampiro: A Máscara V5 (Livro base), pp. 306–308";
            Status = "Informe a parada e Fome da cena e role o teste. Em caso de sucesso, confirme a quantidade e a fonte do sangue na alimentação.";
        }
        catch (ArgumentException exception) { Status = exception.Message; }
    }

    [RelayCommand]
    private void ResolveDiablerie()
    {
        if (!SoulConsumed) { Status = "Confirme que a alma foi consumida antes de resolver as consequências."; return; }
        try
        {
            var result = VCS.Rules.V5.Blood.DiablerieRules.Resolve(DiableristHumanity, DiableristGeneration,
                VictimGeneration, DiableristSuccesses, VictimSuccesses);
            Kind = "Diablerie";
            if (string.IsNullOrWhiteSpace(Title)) Title = "Diablerie";
            Outcome += $"{Environment.NewLine}Humanidade resultante {result.Humanity}; geração {result.Generation}; " +
                $"{result.RestrictedExperience} XP restritos para Potência do Sangue (até o limite da vítima) ou Disciplinas conhecidas pela vítima; " +
                $"veias na aura por {result.AuraYears} ano(s).";
            if (result.Possessed) Outcome += " Humanidade zero: a personalidade da vítima toma o corpo; personagem torna-se NPC.";
            Source = "Vampiro: A Máscara V5 (Livro base), pp. 234–235";
            Status = "Consequências calculadas no evento. Confira as alterações com a mesa antes de editar a ficha.";
        }
        catch (ArgumentException exception) { Status = exception.Message; }
    }

    private bool _loaded;
    private bool _clearParticipant;
    public ObservableCollection<ChronicleEvent> Events { get; } = [];
    public ObservableCollection<CharacterSummary> Participants { get; } = [];
    public string[] Kinds { get; } = ["Caça", "Conflito", "Compulsão", "Máculas e Remorso", "Diablerie", "Vínculo de Sangue", "Torpor", "Discrasia", "Perdição", "Domínio e Coterie", "Exceção do Narrador", "Outro"];
    [ObservableProperty] private ChronicleEvent? selectedEvent;
    [ObservableProperty] private CharacterSummary? participant;
    [ObservableProperty] private DateTime? date = DateTime.Today;
    [ObservableProperty] private string chronicle = "";
    [ObservableProperty] private string kind = "Caça";
    [ObservableProperty] private string title = "";
    [ObservableProperty] private string notes = "";
    [ObservableProperty] private string outcome = "";
    [ObservableProperty] private string source = "Decisão do Narrador";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private bool isBusy;

    public async Task LoadAsync(IEnumerable<CharacterSummary> characters)
    {
        Participants.Clear();
        foreach (var character in characters) Participants.Add(character);
        if (_loaded) return;
        var events = await store.LoadAsync();
        foreach (var entry in events.OrderByDescending(x => x.Date)) Events.Add(entry);
        _loaded = true;
    }

    partial void OnSelectedEventChanged(ChronicleEvent? value)
    {
        if (value is null) return;
        _clearParticipant = false;
        Date = value.Date; Chronicle = value.Chronicle; Kind = value.Kind; Title = value.Title;
        Notes = value.Notes; Outcome = value.Outcome; Source = value.Source;
        Participant = Participants.FirstOrDefault(x => x.Id == value.CharacterId);
    }

    [RelayCommand]
    private void NewEvent()
    {
        SelectedEvent = null; Participant = null; Date = DateTime.Today;
        _clearParticipant = true;
        Title = ""; Notes = ""; Outcome = ""; Source = "Decisão do Narrador"; Status = "Novo evento.";
    }

    [RelayCommand]
    private void ClearParticipant()
    {
        Participant = null;
        _clearParticipant = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(Title) || Date is null) { Status = "Informe título e data."; return; }
        IsBusy = true;
        try
        {
            var previous = SelectedEvent;
            var entry = new ChronicleEvent(previous?.Id ?? Guid.NewGuid(), Date.Value, Chronicle.Trim(), Kind,
                Title.Trim(), Participant?.Id ?? (_clearParticipant ? null : previous?.CharacterId),
                Participant?.Name ?? (_clearParticipant ? "" : previous?.CharacterName ?? ""), Notes, Outcome, Source.Trim());
            var updated = Events.Where(x => x.Id != entry.Id).Append(entry).OrderByDescending(x => x.Date).ToArray();
            await store.SaveAsync(updated);
            Events.Clear();
            foreach (var item in updated) Events.Add(item);
            SelectedEvent = entry;
            Status = "Evento salvo. A ficha do personagem não foi alterada.";
        }
        catch (Exception exception) { Status = $"Não foi possível salvar: {exception.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (IsBusy || SelectedEvent is not { } entry) return;
        IsBusy = true;
        try
        {
            var updated = Events.Where(x => x.Id != entry.Id).ToArray();
            await store.SaveAsync(updated);
            Events.Remove(entry); NewEvent(); Status = "Evento excluído.";
        }
        catch (Exception exception) { Status = $"Não foi possível excluir: {exception.Message}"; }
        finally { IsBusy = false; }
    }
}
