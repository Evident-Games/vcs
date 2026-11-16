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
    [ObservableProperty] private bool sceneBeastSubdued;

    [RelayCommand]
    private void RollScene()
    {
        if (Pool is < 1 or > 50 || RollHunger is < 0 or > 5 || Difficulty is < 1 or > 50)
        {
            Status = "Use parada 1–50, Fome 0–5 e dificuldade 1–50.";
            return;
        }
        var beastSubdued = SceneBeastSubdued;
        var result = new VCS.Application.Dice.DiceActions().Roll(Pool, RollHunger, Difficulty, beastSubdued: beastSubdued);
        SceneBeastSubdued = false;
        var label = result.Outcome switch
        {
            VCS.Rules.V5.Dice.TestOutcome.MessyCritical => "crítico bestial",
            VCS.Rules.V5.Dice.TestOutcome.BestialFailure => "falha bestial",
            VCS.Rules.V5.Dice.TestOutcome.CriticalSuccess => "sucesso crítico",
            VCS.Rules.V5.Dice.TestOutcome.Success => "sucesso",
            _ => "falha"
        };
        Outcome += $"{Environment.NewLine}{Kind}: {label}; {result.Successes} sucessos / dificuldade {Difficulty}. " +
            $"Comuns [{string.Join(", ", result.NormalDice)}]; Fome [{string.Join(", ", result.HungerDice)}]." +
            (beastSubdued ? " Besta subjugada confirmada pelo Narrador." : "");
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

    [ObservableProperty] private string counterparty = "";
    [ObservableProperty] private int bondLevel;
    [ObservableProperty] private DateTime? lastDrink;
    [ObservableProperty] private string effect = "";
    [ObservableProperty] private string discipline = "";
    [ObservableProperty] private string expiration = "";
    [ObservableProperty] private bool active = true;
    [ObservableProperty] private int round = 1;
    [ObservableProperty] private int firstPool = 5;
    [ObservableProperty] private int secondPool = 5;
    [ObservableProperty] private int firstHunger;
    [ObservableProperty] private int secondHunger;
    [ObservableProperty] private int firstWeapon;
    [ObservableProperty] private int secondWeapon;
    [ObservableProperty] private bool firstBeastSubdued;
    [ObservableProperty] private bool secondBeastSubdued;

    [RelayCommand]
    private void RecordVitae()
    {
        if (Participant is null || string.IsNullOrWhiteSpace(Counterparty) || Date is null)
        { Status = "Selecione o personagem, informe a outra parte e a data."; return; }
        try
        {
            var state = new VCS.Rules.V5.Blood.BloodBondState(Counterparty, Participant.Id.ToString(), BondLevel,
                LastDrink is { } last ? DateOnly.FromDateTime(last) : null);
            var result = new ChronicleActions().RecordVitae(state, DateOnly.FromDateTime(Date.Value));
            BondLevel = result.State.Level;
            LastDrink = result.State.LastVitaeDate?.ToDateTime(TimeOnly.MinValue);
            Kind = "Vínculo de Sangue";
            if (string.IsNullOrWhiteSpace(Title)) Title = $"Vínculo: {Participant.Name} / {Counterparty}";
            Outcome += $"{Environment.NewLine}Gole registrado em {Date:dd/MM/yyyy}: nível {BondLevel}; novo gole contado: {result.CountedAsNewDrink}.";
            Source = "Vampiro: A Máscara V5 (Livro base), pp. 233–234";
            Status = "Vínculo atualizado no rascunho. Salve o evento.";
        }
        catch (ArgumentException exception) { Status = exception.Message; }
    }

    [RelayCommand]
    private void ResolveRound()
    {
        if (FirstPool is < 1 or > 50 || SecondPool is < 1 or > 50 || FirstHunger is < 0 or > 5 || SecondHunger is < 0 or > 5 ||
            FirstWeapon is < -20 or > 20 || SecondWeapon is < -20 or > 20 || Round < 1)
        { Status = "Confira as paradas (1–50), Fome (0–5), modificadores (−20 a 20) e turno."; return; }
        var dice = new VCS.Application.Dice.DiceActions();
        var firstSubdued = FirstBeastSubdued;
        var secondSubdued = SecondBeastSubdued;
        var first = dice.Roll(FirstPool, FirstHunger, 1, beastSubdued: firstSubdued);
        var second = dice.Roll(SecondPool, SecondHunger, 1, beastSubdued: secondSubdued);
        FirstBeastSubdued = false;
        SecondBeastSubdued = false;
        var result = new ChronicleActions().ResolveConflict(VCS.Rules.V5.Conflicts.ConflictKind.Physical,
            first, FirstWeapon, second, SecondWeapon, VCS.Rules.V5.Conflicts.ConflictDamageType.Superficial);
        Kind = "Conflito";
        if (string.IsNullOrWhiteSpace(Title)) Title = "Conflito";
        Outcome += $"{Environment.NewLine}Turno {Round}: {first.Successes} vs {second.Successes} sucessos; " +
            $"dano bruto lado 1: {result.Exchange.Damage.ToFirst}, lado 2: {result.Exchange.Damage.ToSecond}. " +
            $"Resultados: {result.FirstOutcome} / {result.SecondOutcome}." +
            (firstSubdued ? " Besta subjugada no lado 1 confirmada pelo Narrador." : "") +
            (secondSubdued ? " Besta subjugada no lado 2 confirmada pelo Narrador." : "");
        Round++;
        Status = "Turno registrado. Confirme tipo de dano, redução, manobra e aplicação nas fichas conforme a cena.";
    }

    private ChronicleEventState CaptureState() => new(Counterparty, BondLevel, LastDrink, Effect, Discipline,
        Expiration, Active, Round, FirstPool, SecondPool, FirstHunger, SecondHunger, FirstWeapon, SecondWeapon,
        Formula, Ingredients, Resonance, Alchemy, FormulaLevel, AlchemyAttribute, IngredientModifier, Method, Improvised);

    private void RestoreState(ChronicleEventState state)
    {
        SceneBeastSubdued = false; FirstBeastSubdued = false; SecondBeastSubdued = false;
        Formula = state.Formula; Ingredients = state.Ingredients; Resonance = state.Resonance;
        Alchemy = state.Alchemy; FormulaLevel = state.FormulaLevel; AlchemyAttribute = state.AlchemyAttribute;
        IngredientModifier = state.IngredientModifier; Method = state.Method; Improvised = state.Improvised;
        FormulaExceptionApproved = false;
        Counterparty = state.Counterparty; BondLevel = state.BondLevel; LastDrink = state.LastDrink;
        Effect = state.Effect; Discipline = state.Discipline; Expiration = state.Expiration; Active = state.Active;
        Round = state.Round; FirstPool = state.FirstPool; SecondPool = state.SecondPool;
        FirstHunger = state.FirstHunger; SecondHunger = state.SecondHunger;
        FirstWeapon = state.FirstWeapon; SecondWeapon = state.SecondWeapon;
    }

    public string[] AlchemyMethods { get; } = ["AthanorCorporis", "Calcinatio", "Fixatio"];
    [ObservableProperty] private string formula = "";
    [ObservableProperty] private string ingredients = "";
    [ObservableProperty] private string resonance = "";
    [ObservableProperty] private int alchemy = 1;
    [ObservableProperty] private int formulaLevel = 1;
    [ObservableProperty] private int alchemyAttribute = 1;
    [ObservableProperty] private int ingredientModifier;
    [ObservableProperty] private string method = "AthanorCorporis";
    [ObservableProperty] private bool improvised;
    [ObservableProperty] private bool formulaExceptionApproved;

    [RelayCommand]
    private void DistillFormula()
    {
        if (FormulaLevel > Alchemy && !FormulaExceptionApproved)
        { Status = "Fórmula acima do nível de Alquimia: requer autorização do Narrador."; return; }
        if (string.IsNullOrWhiteSpace(Formula) || string.IsNullOrWhiteSpace(Ingredients) || string.IsNullOrWhiteSpace(Resonance))
        { Status = "Informe fórmula, ingredientes e Ressonância necessária."; return; }
        try
        {
            var pool = VCS.Rules.V5.Alchemy.ThinBloodAlchemyRules.Pool(AlchemyAttribute, Alchemy, IngredientModifier,
                Method == "Fixatio" && Improvised);
            var roll = new VCS.Application.Dice.DiceActions().Roll(pool, 0, 1);
            var resolution = VCS.Rules.V5.Alchemy.ThinBloodAlchemyRules.Resolve(AlchemyAttribute, Alchemy,
                FormulaLevel, IngredientModifier, Method == "Fixatio" && Improvised, roll.Successes);
            Kind = "Sangue-Ralo";
            if (string.IsNullOrWhiteSpace(Title)) Title = Formula;
            Outcome += $"{Environment.NewLine}{Formula} ({Method}), {pool} dados [{string.Join(", ", roll.NormalDice)}]: " +
                $"{roll.Successes} sucessos. {resolution.Effect} Custo de aprendizado: {resolution.FormulaCost} XP; não debitado automaticamente. " +
                "Destilação requer uma Checagem de Sangue; a ativação paga o custo do poder imitado. Registre os pagamentos na ficha.";
            Source = "Vampiro: A Máscara V5 (Livro base), pp. 282–284";
            Status = "Destilação registrada no rascunho. Salve o evento para preservar fórmula e ingredientes.";
        }
        catch (ArgumentException exception) { Status = exception.Message; }
    }

    public Func<ChronicleEvent, Task<bool>>? ApplyToCharacter { get; set; }

    [RelayCommand]
    private async Task ApplyToSheetAsync()
    {
        if (IsBusy || SelectedEvent is not { CharacterId: not null } entry || ApplyToCharacter is null)
        { Status = "Salve e selecione um evento associado a um personagem com a ficha aberta."; return; }
        IsBusy = true;
        try
        {
            Status = await ApplyToCharacter(entry)
                ? "Evento aplicado e salvo na ficha selecionada."
                : "Abra a ficha do personagem associado e confira eventuais erros de salvamento.";
        }
        catch (Exception exception) { Status = $"Não foi possível aplicar: {exception.Message}"; }
        finally { IsBusy = false; }
    }

    private bool _loaded;
    private bool _clearParticipant;
    public ObservableCollection<ChronicleEvent> Events { get; } = [];
    public ObservableCollection<CharacterSummary> Participants { get; } = [];
    public string[] Kinds { get; } = ["Caça", "Conflito", "Compulsão", "Máculas e Remorso", "Diablerie", "Vínculo de Sangue", "Torpor", "Discrasia", "Perdição", "Domínio e Coterie", "Sangue-Ralo", "Exceção do Narrador", "Outro"];
    [ObservableProperty] private ChronicleEvent? selectedEvent;
    [ObservableProperty] private CharacterSummary? participant;
    partial void OnParticipantChanged(CharacterSummary? value) => SceneBeastSubdued = false;
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
        RestoreState(value.State ?? new());
        Date = value.Date; Chronicle = value.Chronicle; Kind = value.Kind; Title = value.Title;
        Notes = value.Notes; Outcome = value.Outcome; Source = value.Source;
        Participant = Participants.FirstOrDefault(x => x.Id == value.CharacterId);
    }

    [RelayCommand]
    private void NewEvent()
    {
        SelectedEvent = null; Participant = null; Date = DateTime.Today;
        RestoreState(new());
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
                Participant?.Name ?? (_clearParticipant ? "" : previous?.CharacterName ?? ""), Notes, Outcome, Source.Trim(), CaptureState());
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
