using VCS.Application.Chronicle;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    /// <summary>Copies an explicitly selected Storyteller ruling to its character without replaying numeric costs.</summary>
    public void ApplyNarratorEvent(ChronicleEvent entry)
    {
        if (entry.CharacterId != Id) throw new ArgumentException("O evento pertence a outro personagem.", nameof(entry));
        var title = $"Evento do Narrador · {entry.Id}";
        var text = $"{entry.Date:dd/MM/yyyy} · {entry.Kind} · {entry.Title}{Environment.NewLine}" +
            $"{entry.Notes}{Environment.NewLine}{entry.Outcome}{Environment.NewLine}Fonte: {entry.Source}";
        if (entry.State is { } state)
        {
            text += $"{Environment.NewLine}Efeito: {state.Effect}; disciplina: {state.Discipline}; término/consumo: {state.Expiration}; ativo: {state.Active}.";
            if (entry.Kind == "Discrasia")
                _extraFields["Dyscrasia"].Value = state.Active
                    ? $"{state.Effect}; {state.Discipline}; {state.Expiration}; fonte: {entry.Source}" : "";
            if (entry.Kind == "Perdição" && state.Active)
                _extraFields["ClanBane"].Value = $"{state.Effect}; fonte: {entry.Source}";
        }
        var existing = Notebook.Notes.FirstOrDefault(x => x.Title == title);
        if (existing is null) Notebook.AppendLog(title, text);
        else existing.Text = text;
        RefreshRollPool();
    }
}
