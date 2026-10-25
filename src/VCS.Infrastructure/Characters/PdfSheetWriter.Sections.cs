using System.Globalization;
using PdfSharp.Drawing;
using VCS.Application.Catalogs;
using VCS.Application.Characters;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Blood;
using VCS.Domain.Characters;
using VCS.Domain.Predators;
using VCS.Domain.Skills;

namespace VCS.Infrastructure.Characters;

/// <summary>Sheet content: one method per section of the character or NPC sheet.</summary>
internal sealed partial class PdfSheetWriter
{
    public void Write(IReadOnlyList<PdfSection> sections)
    {
        if (_character.Details.IsNpc)
        {
            // The NPC PDF always mirrors the whole NPC sheet, so section choices do not apply.
            NpcQuickSheet();
            return;
        }
        foreach (var section in sections)
        {
            Heading(section.Title);
            switch (section.Key)
            {
                case SheetSection.Identity: Identity(_character.Details.IsNpc); break;
                case SheetSection.Attributes:
                    if (_character.Details.IsNpc)
                    {
                        Rating("Fisicos", _character.Details.NpcPhysical, 5);
                        Rating("Sociais", _character.Details.NpcSocial, 5);
                        Rating("Mentais", _character.Details.NpcMental, 5);
                        break;
                    }
                    foreach (var reference in CharacterRules.DefaultAttributes)
                        Rating(reference.Name, _character.Traits.Single(x => x.Category == TraitCategory.Attribute && x.Key == reference.Key).Rating, 5);
                    break;
                case SheetSection.Skills: Skills(); break;
                case SheetSection.Disciplines: Disciplines(); break;
                case SheetSection.Advantages: Advantages(); break;
                case SheetSection.State: State(); break;
                case SheetSection.Background:
                    if (_character.Details.IsNpc)
                    {
                        Text("Conceito", _character.Details.Concept);
                        break;
                    }
                    foreach (var property in new[] { "ChronicleTenets", "Touchstones", "Convictions", "ClanBane", "TrueAge",
                        "ApparentAge", "BirthDate", "DeathDate", "Appearance", "DistinguishingFeatures", "History" })
                        Detail(property);
                    Text("Experiencia total", _character.Details.ExperienceTotal.ToString(CultureInfo.InvariantCulture));
                    Text("Experiencia gasta", _character.Details.ExperienceSpent.ToString(CultureInfo.InvariantCulture));
                    break;
                case SheetSection.Notes: Notes(); break;
                default: throw new InvalidDataException($"Secao PDF desconhecida: {section.Key}.");
            }
            if (_character.Details.Advancement is { } advancement)
            {
                if (advancement.AutomaticExperience && section.Key != SheetSection.Background) continue;
                Heading(advancement.AutomaticExperience ? "Experiencia" : "Criacao e evolucao / XP");
                if (!advancement.AutomaticExperience)
                    Text("Etapa", advancement.IsCreating ? "Em criacao" : "Evolucao");
                if (advancement.HasCreationProfile)
                    Text("Idade vampirica", advancement.Age switch { VampireAge.Child => "Crianca da noite", VampireAge.Neonate => "Neofito", _ => "Ancillae" });
                Text("XP total", _character.Details.ExperienceTotal.ToString(CultureInfo.InvariantCulture));
                Text("Total gasto de XP", _character.Details.ExperienceSpent.ToString(CultureInfo.InvariantCulture));
                Text("XP disponivel", (_character.Details.ExperienceTotal - _character.Details.ExperienceSpent).ToString(CultureInfo.InvariantCulture));
                Text("XP de campanha recebido", _character.Details.CampaignExperienceTotal.ToString(CultureInfo.InvariantCulture));
                Text("XP de campanha gasto", _character.Details.CampaignExperienceSpent.ToString(CultureInfo.InvariantCulture));
                Text("Saldo de XP de campanha", LoresheetRules.CampaignAvailable(_character.Details).ToString(CultureInfo.InvariantCulture));
                if (advancement.AutomaticExperience)
                {
                    var calculation = AutomaticExperience.Calculate(_character.Details, _character.Traits);
                    Text("Custos de XP", string.Join(Environment.NewLine, calculation.Costs.Select(x => $"{x.Name}: {x.Cost} XP.")), true);
                    Text("Avisos de XP", string.Join(Environment.NewLine, calculation.Warnings.Select(x => x.Message)), true);
                }
                else
                    Text("Historico de XP", string.Join(Environment.NewLine, advancement.Purchases.Select(x =>
                        $"{x.Utc:yyyy-MM-dd HH:mm} UTC | {x.Name}: {(x.Specialization.Length == 0 ? $"{x.From} -> {x.To}" : x.Specialization)} | {x.Cost} XP ({x.CampaignCost} campanha)")), true);
            }
        }
    }

    private void Identity(bool npc)
    {
        if (npc) { Heading("Identidade / NPC"); PredatorReference(); return; }
        Text("Nome", _character.Details.Name);
        Text("Cla", _character.Details.Clan);
        Text("Geracao", _character.Details.Generation.ToString(CultureInfo.InvariantCulture));
        foreach (var property in new[] { "MortalName", "Sire", "Concept", "Chronicle", "Description", "Ambition", "Desire" })
            Detail(property);
        PredatorReference();
    }

    private void PredatorReference()
    {
        if (_character.Details.PredatorSelection is not { } selection)
        {
            return;
        }
        var definition = PredatorCatalog.Find(selection.Id)!;
        Text("Predador aplicado", definition.Name);
        Text("Descricao do predador", definition.Description, true);
        Text("Disciplina de predador", selection.Discipline.Length > 0 ? $"{selection.Discipline}: +1" : "Nao escolhida");
        Text("Especializacao de predador", selection.Skill.Length > 0 ? $"{selection.Skill} ({selection.Specialization})" : "Nao escolhida");
        Text("Ajustes iniciais do predador", $"Humanidade {selection.HumanityDelta:+0;-0;0}; Potencia de Sangue {selection.BloodDelta:+0;-0;0}");
        Text("Vantagens e defeitos adicionais do predador", string.Join(Environment.NewLine,
            selection.Grants.Select(x => $"{(x.Category == TraitCategory.Flaw ? "Defeito" : "Vantagem")}: {x.Name} ({x.Rating})")), true);
        Text("Beneficios do predador", definition.Benefits, true);
        Text("Custos do predador", definition.Cost, true);
    }

    private void Skills()
    {
        if (_character.Details.IsNpc) Heading("Habilidades");
        var used = new HashSet<string>();
        var groups = _skills.GroupBy(x => x.Group).OrderBy(x => x.Key == "Físicas" ? 0 : x.Key == "Sociais" ? 1 : 2).ToArray();
        foreach (var batch in groups.Chunk(3))
        {
            Ensure(70);
            var columns = batch.Select(x => x.OrderBy(s => s.Name).ToArray()).ToArray();
            var rowCount = columns.Max(x => x.Length);
            for (var row = 0; row < rowCount; row++)
            {
                TraitValue? Value(SkillDefinition entry) => _character.Traits.FirstOrDefault(x => x.Category == TraitCategory.Skill &&
                    (x.Key == entry.Id || CatalogText.Normalize(x.Name) == CatalogText.Normalize(entry.Name)));
                var rowHeight = 52 + columns.Where(x => row < x.Length)
                    .Max(x => Lines(Inline(Value(x[row])?.Specialization ?? ""), 155).Count) * 14 + 12;
                if (row == 0 || _y + rowHeight > Bottom)
                {
                    Ensure(rowHeight + 28);
                    for (var column = 0; column < batch.Length; column++)
                        _graphics.DrawString(batch[column].Key, _heading, XBrushes.Black, Left + column * 175, _y + 18);
                    _y += 28;
                }
                Ensure(rowHeight);
                for (var column = 0; column < columns.Length; column++)
                {
                    if (row >= columns[column].Length) continue;
                    var entry = columns[column][row];
                    var value = Value(entry);
                    if (value is not null) used.Add(value.Key);
                    var x = Left + column * 175;
                    _graphics.DrawString(entry.Name, _font, XBrushes.Black, x, _y + 12);
                    for (var dot = 1; dot <= 5; dot++)
                        Checkbox($"{entry.Name} / {dot}", x + (dot - 1) * 23, _y + 20, dot <= (value?.Rating ?? 0), true);
                    var specialization = value?.Specialization ?? "";
                    var lines = Lines(Inline(specialization), 155);
                    var height = lines.Count * 14 + 8;
                    var field = Widget($"Especializacao de {entry.Name}", x, _y + 42, 165, height, "/Tx");
                    field.Elements.SetString("/V", specialization);
                    field.Elements.SetString("/DV", specialization);
                    field.Elements.SetInteger("/Ff", 4096);
                    SetAppearance(field, Appearance(165, height, g =>
                    {
                        g.DrawRectangle(XPens.LightGray, 0, 0, 165, height);
                        for (var i = 0; i < lines.Count; i++)
                            g.DrawString(lines[i], _font, XBrushes.Black, 4, 14 + i * 14);
                    }));
                }
                _y += rowHeight;
            }
        }
        foreach (var extra in _character.Traits.Where(x => x.Category == TraitCategory.Skill && !used.Contains(x.Key)))
        {
            if (extra.Skill is { } skillGroup) Text("Grupo da habilidade", skillGroup.Group);
            Rating(extra.Name, extra.Rating, 5);
            Text($"Especializacao de {extra.Name}", extra.Specialization, true);
        }
    }

    private static string Inline(string value) => value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');

    private void Disciplines()
    {
        if (_character.Details.IsNpc) Heading("Disciplinas");
        var entries = _character.Traits.Where(x => x.Category == TraitCategory.Discipline).ToArray();
        if (_character.Details.IsNpc)
            entries = [.. entries.Where(x => x.Rating > 0 || !_powers.Powers.Any(p => !p.IsRitual &&
                CatalogText.Normalize(p.Discipline) == CatalogText.Normalize(x.Name))).OrderByDescending(x => x.Rating)];
        foreach (var entry in entries) Rating(entry.Name, entry.Rating, 5);
        if (_character.Details.IsNpc)
            for (var i = entries.Length; i < 3; i++) { Text($"Disciplina {i + 1}", ""); Rating($"Nivel {i + 1}", 0, 5); }
        foreach (var entry in _character.Traits.Where(x => x.Category == TraitCategory.DisciplinePower))
        {
            var definition = _powers.Powers.FirstOrDefault(x => x.Id == entry.Key);
            if (entry.Power is { } power)
            {
                static string Or(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
                Heading($"{entry.Name} / {(power.IsRitual ? "Ritual" : "Poder")}");
                Text("Disciplina", Or(power.Discipline, "Nao informada"));
                Text("Nivel", entry.Rating.ToString(CultureInfo.InvariantCulture));
                Text("Pre-requisito", Or(power.Prerequisite, "Nenhum."), true);
                Text("Custo", Or(power.Cost, "Nao informado."), true);
                Text("Parada de dados", Or(power.DicePool, "Nao informada."), true);
                Text("Duracao", Or(power.Duration, "Nao informada."), true);
                if (power.IsRitual)
                {
                    Text("Ingredientes", Or(power.Ingredients, "Nao informados."), true);
                    Text("Processo", Or(power.Process, "Nao informado."), true);
                    Text("Sistema", Or(power.System, "Nao informado."), true);
                }
                Text("Descricao", Or(power.Description, "Nao informada."), true);
                continue;
            }
            Heading($"{entry.Name} / {(definition?.IsRitual == true ? "Ritual" : "Poder")}");
            Text("Disciplina", definition?.Discipline ?? "Fora do catalogo");
            Text("Nivel", entry.Rating.ToString(CultureInfo.InvariantCulture));
            if (definition?.AmalgamDiscipline is { } amalgam)
                Text("Amalgama", $"{amalgam} {definition.AmalgamLevel}");
            Text("Descricao base", definition?.BaseDescription ?? "Nao informada.", true);
            Text("Ingredientes", definition?.Ingredients ?? "Nao informados.", true);
            Text("Custo", definition?.Cost ?? "Nao informado.", true);
            Text("Parada de dados", definition?.DicePool ?? "Nao informada.", true);
            Text("Como funciona", definition?.Summary ?? "Entrada preservada, sem referencia disponivel.", true);
        }
    }

    private void Advantages()
    {
        if (_character.Details.IsNpc) Heading("Vantagens e defeitos");
        var entries = _character.Traits.Where(x => x.Category is TraitCategory.Advantage or TraitCategory.Flaw).ToArray();
        foreach (var entry in entries)
        {
            Rating($"{(entry.Category == TraitCategory.Flaw ? "Defeito" : "Vantagem")}: {entry.Name}", entry.Rating, 5);
            if (entry.Merit is { } merit)
            {
                Text("Familia", merit.Group);
                Text($"Descricao de {entry.Name}", merit.Description, true);
            }
        }
        if (_character.Details.IsNpc)
            for (var i = entries.Length; i < 6; i++) { Text($"Vantagem / defeito {i + 1}", ""); Rating($"Pontos {i + 1}", 0, 5); }
        var loresheets = _character.Traits.Where(x => x.Category == TraitCategory.Loresheet).ToArray();
        if (loresheets.Length == 0) return;
        Heading("Loresheets / Fichas de Conhecimento");
        foreach (var entry in loresheets)
        {
            var lore = entry.Loresheet!;
            Text("Colecao", $"{lore.Collection} / {(lore.IsLineage ? "Linhagem/descendencia" : "Geral")}");
            Text("Beneficio", entry.Name);
            Text("Custo individual", $"{entry.Rating} ponto(s)");
            Text("Pagamento", lore.Payment switch
            {
                LoresheetPayment.AdvantagePoints => $"{entry.Rating} pontos iniciais de vantagens",
                LoresheetPayment.InitialXp => $"{entry.Rating * 3} XP inicial de idade",
                _ => $"{entry.Rating * 3} XP de campanha"
            });
            Text("Descricao do beneficio", lore.Description, true);
        }
    }

    // Mirrors the NPC sheet in the app: base, combat, attributes, skills, disciplines, merits and background.
    private void NpcQuickSheet()
    {
        var details = _character.Details;
        Heading("Ficha de NPC");
        Text("Nome", details.Name);
        Text("Cronica", details.Chronicle);
        Text("Cla", details.Clan);
        Text("Geracao", details.Generation.ToString(CultureInfo.InvariantCulture));
        Heading("Combate");
        Damage("Vitalidade", details.NpcHealthCapacity, details.HealthSuperficial, details.HealthAggravated, false);
        Damage("Forca de Vontade", details.NpcWillpowerCapacity, details.WillpowerSuperficial, details.WillpowerAggravated, true);
        Rating("Potencia de Sangue", details.BloodPotency, 10);
        Detail("BloodSurge");
        Detail("DamageMend");
        Detail("PowerBonus");
        Rating("Fome", details.Hunger, 5, false);
        Heading("Ataques");
        var number = 0;
        foreach (var attack in details.NpcAttacks)
        {
            number++;
            Text($"Ataque {number}", attack.Name);
            Text($"Ataque {number} - Parada de dados", attack.DicePool);
            Text($"Ataque {number} - Bonus de dano", attack.DamageBonus);
            Text($"Ataque {number} - Tipo de dano", attack.DamageType);
            Text($"Ataque {number} - Disciplinas ativas", attack.ActiveDisciplines);
            Text($"Ataque {number} - Notas", attack.Notes, true);
        }
        if (number == 0) Text("Ataque 1", "");
        Heading("Atributos");
        Rating("Fisicos", details.NpcPhysical, 5);
        Rating("Sociais", details.NpcSocial, 5);
        Rating("Mentais", details.NpcMental, 5);
        Skills();
        Disciplines();
        Advantages();
        Heading("Historico");
        Detail("TrueAge");
        Detail("ApparentAge");
        Text("Notas", details.Notes, true);
        Detail("History");
        Detail("Appearance");
        PredatorReference();
    }

    private void Notes()
    {
        if (!string.IsNullOrWhiteSpace(_character.Details.Notes)) Text("Notas", _character.Details.Notes, true);
        foreach (var note in _character.Details.NoteEntries) Text(string.IsNullOrWhiteSpace(note.Title) ? "Nota" : note.Title, note.Text, true);
    }

    private void State()
    {
        var details = _character.Details;
        if (details.IsNpc) Heading("Estado");
        var health = details.IsNpc ? details.NpcHealthCapacity : CharacterRules.HealthCapacity(_character.Traits);
        var willpower = details.IsNpc ? details.NpcWillpowerCapacity : CharacterRules.WillpowerCapacity(_character.Traits);
        Damage("Vitalidade", health, details.HealthSuperficial, details.HealthAggravated, false);
        Damage("Forca de Vontade", willpower, details.WillpowerSuperficial, details.WillpowerAggravated, details.IsNpc);
        Rating("Fome", details.Hunger, 5, false);
        Rating("Humanidade", details.Humanity, 10, false);
        Text("Maculas", details.Stains.ToString(CultureInfo.InvariantCulture));
        Text("Efeitos da Humanidade atual", HumanityRules.At(details.Humanity).Description, true);
        Text("Maculas e Remorso", "A mesa determina quando Maculas causam dano Agravado na Forca de Vontade. Falha em Remorso limpa as Maculas e reduz a Humanidade em 1. Nenhuma rolagem automatica.", true);
        Rating("Potencia de Sangue", details.BloodPotency, 10);
        if (!details.IsNpc)
        {
            foreach (var property in new[] { "Resonance", "BloodSurge", "DamageMend", "PowerBonus", "RouseReroll", "FeedingPenalty", "BaneSeverity" })
                Detail(property);
        }
    }

    private void Detail(string property)
    {
        var names = new Dictionary<string, string>
        {
            ["MortalName"] = "Nome mortal", ["Sire"] = "Sire", ["Predator"] = "Predador", ["Concept"] = "Conceito",
            ["Chronicle"] = "Cronica", ["Description"] = "Descricao", ["Ambition"] = "Ambicao", ["Desire"] = "Desejo",
            ["ChronicleTenets"] = "Principios da cronica", ["Touchstones"] = "Pilares", ["Convictions"] = "Conviccoes",
            ["ClanBane"] = "Perdicao do cla", ["TrueAge"] = "Idade verdadeira", ["ApparentAge"] = "Idade aparente",
            ["BirthDate"] = "Nascimento", ["DeathDate"] = "Morte / Abraco", ["Appearance"] = "Aparencia",
            ["DistinguishingFeatures"] = "Tracos distintivos", ["History"] = "Historia", ["Resonance"] = "Ressonancia",
            ["BloodSurge"] = "Surto de Sangue", ["DamageMend"] = "Quantidade recuperada", ["PowerBonus"] = "Bonus de poder",
            ["RouseReroll"] = "Rerrolagem de Sangue", ["FeedingPenalty"] = "Penalidade de alimentacao", ["BaneSeverity"] = "Gravidade da Perdicao"
        };
        Text(names[property], (string)typeof(CharacterDetails).GetProperty(property)!.GetValue(_character.Details)!,
            property is "Description" or "ChronicleTenets" or "Touchstones" or "Convictions" or "ClanBane"
                or "Appearance" or "DistinguishingFeatures" or "History");
    }
}
