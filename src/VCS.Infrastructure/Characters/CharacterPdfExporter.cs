using System.Globalization;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using VCS.Application.Catalogs;
using VCS.Application.Characters;
using VCS.Domain.Advancement;
using VCS.Domain.Advantages;
using VCS.Domain.Blood;
using VCS.Domain.Characters;
using VCS.Domain.Predators;
using VCS.Domain.Skills;

namespace VCS.Infrastructure.Characters;

public sealed class CharacterPdfExporter(IDisciplineCatalog powers, ISkillCatalog skills) : ICharacterPdfExporter
{
    static CharacterPdfExporter() => GlobalFontSettings.UseWindowsFontsUnderWindows = true;

    public async Task ExportAsync(CharacterDto character, string path, IReadOnlyList<PdfSection> sections,
        CancellationToken cancellationToken = default)
    {
        var issues = CharacterRules.Validate(character.Details, character.Traits);
        if (issues.Count != 0) throw new CharacterValidationException(issues);
        if (!character.Details.IsNpc && sections.Count == 0)
            throw new InvalidDataException("Selecione ao menos uma secao para exportar.");
        var catalog = await skills.ListAsync(cancellationToken);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await Task.Run(() =>
            {
                using var writer = new SheetWriter(character, powers, catalog, cancellationToken);
                writer.Write(sections);
                writer.Save(temporary);
            }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed class SheetWriter : IDisposable
    {
        private readonly CharacterDto _character;
        private readonly IDisciplineCatalog _powers;
        private readonly IReadOnlyList<SkillDefinition> _skills;
        private readonly CancellationToken _cancellation;
        private readonly PdfDocument _document = new();
        private readonly PdfArray _fields;
        private readonly XFont _font = new("Arial", 10);
        private readonly XFont _heading = new("Arial", 15, XFontStyleEx.Bold);
        private PdfPage _page = null!;
        private XGraphics _graphics = null!;
        private double _y;
        private int _id;
        private const double Left = 40;
        private const double Width = 515;
        private const double Bottom = 790;

        public SheetWriter(CharacterDto character, IDisciplineCatalog powers, IReadOnlyList<SkillDefinition> skills, CancellationToken cancellation)
        {
            _character = character;
            _powers = powers;
            _skills = skills;
            _cancellation = cancellation;
            _document.Info.Title = $"VCS - {(character.Details.IsNpc ? "NPC" : "Personagem")} - {character.Details.Name}";
            _document.Info.Creator = "VCS";
            _fields = new PdfArray(_document);
            var form = new PdfDictionary(_document);
            _document.Internals.AddObject(form);
            _document.Internals.Catalog.Elements["/AcroForm"] = form.Reference!;
            form.Elements["/Fields"] = _fields;
            form.Elements.SetBoolean("/NeedAppearances", false);
            var font = new PdfDictionary(_document);
            font.Elements.SetName("/Type", "/Font");
            font.Elements.SetName("/Subtype", "/Type1");
            font.Elements.SetName("/BaseFont", "/Helvetica");
            font.Elements.SetName("/Encoding", "/WinAnsiEncoding");
            _document.Internals.AddObject(font);
            var fonts = new PdfDictionary(_document);
            fonts.Elements["/Helv"] = font.Reference!;
            var resources = new PdfDictionary(_document);
            resources.Elements["/Font"] = fonts;
            form.Elements["/DR"] = resources;
            form.Elements.SetString("/DA", "/Helv 10 Tf 0 g");
            NewPage();
        }

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
                    case "Identity": Identity(_character.Details.IsNpc); break;
                    case "Attributes":
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
                    case "Skills": Skills(); break;
                    case "Disciplines": Disciplines(); break;
                    case "Advantages": Advantages(); break;
                    case "State": State(); break;
                    case "Background":
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
                    case "Notes": Text("Notas", _character.Details.Notes, true); break;
                    default: throw new InvalidDataException($"Secao PDF desconhecida: {section.Key}.");
                }
                if (_character.Details.Advancement is { } advancement)
                {
                    if (advancement.AutomaticExperience && section.Key != "Background") continue;
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

        private void NewPage()
        {
            _graphics?.Dispose();
            _page = _document.AddPage();
            _page.Size = PdfSharp.PageSize.A4;
            _graphics = XGraphics.FromPdfPage(_page);
            _graphics.DrawString($"VCS / {(_character.Details.IsNpc ? "NPC" : "Personagem")}", _heading, XBrushes.Black, Left, 30);
            _graphics.DrawString($"Pagina {_document.PageCount} - Campos preenchiveis; alteracoes nao retornam ao aplicativo.", _font, XBrushes.Gray, Left, 823);
            _y = 50;
        }

        private void Ensure(double height)
        {
            _cancellation.ThrowIfCancellationRequested();
            if (_y + height > Bottom) NewPage();
        }

        private void Heading(string title)
        {
            foreach (var line in Lines(title, Width, _heading))
            {
                Ensure(36);
                _graphics.DrawString(line, _heading, XBrushes.Black, Left, _y + 19);
                _y += 30;
            }
        }

        private List<string> Lines(string value, double width, XFont? font = null)
        {
            var result = new List<string>();
            foreach (var paragraph in value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                var line = "";
                foreach (var rune in paragraph.EnumerateRunes())
                {
                    var next = rune.ToString();
                    if (line.Length != 0 && _graphics.MeasureString(line + next, font ?? _font).Width > width)
                    {
                        result.Add(line);
                        line = "";
                    }
                    line += next;
                }
                result.Add(line);
            }
            return result;
        }

        private void Text(string label, string value, bool multiline = false)
        {
            var lines = Lines(value, Width - 10);
            var offset = 0;
            do
            {
                Ensure(multiline && value.Length == 0 ? 82 : 60);
                var count = Math.Min(lines.Count - offset, Math.Max(1, (int)((Bottom - _y - 34) / 14)));
                var chunk = lines.Skip(offset).Take(count).ToArray();
                var height = Math.Max(multiline && value.Length == 0 ? 56 : 22, chunk.Length * 14 + 8);
                _graphics.DrawString(label + (offset == 0 ? "" : " (continuacao)"), _font, XBrushes.Black, Left, _y + 11);
                _y += 16;
                var field = Widget(label, Left, _y, Width, height, "/Tx");
                field.Elements.SetString("/V", string.Join("\n", chunk));
                field.Elements.SetString("/DV", string.Join("\n", chunk));
                field.Elements.SetInteger("/Ff", multiline || lines.Count > 1 ? 4096 : 0);
                SetAppearance(field, Appearance(Width, height, g =>
                {
                    g.DrawRectangle(XPens.LightGray, 0, 0, Width, height);
                    for (var i = 0; i < chunk.Length; i++) g.DrawString(chunk[i], _font, XBrushes.Black, 4, 14 + i * 14);
                }));
                _y += height + 10;
                offset += count;
            } while (offset < lines.Count);
        }

        private void Rating(string label, int rating, int maximum, bool round = true)
        {
            var labels = Lines(label, 270);
            for (var n = 0; n < labels.Count; n++)
            {
                Ensure(30);
                _graphics.DrawString(labels[n], _font, XBrushes.Black, Left, _y + 16);
                if (n < labels.Count - 1) _y += 16;
            }
            for (var i = 1; i <= maximum; i++)
                Checkbox($"{label} / {i}", Left + 280 + (i - 1) * 22, _y + 2, i <= rating, round);
            _y += 27;
        }

        private void Checkbox(string label, double x, double y, bool filled, bool round)
        {
            var field = Widget(label, x, y, 16, 16, "/Btn");
            var appearances = new PdfDictionary(_document);
            appearances.Elements["/Off"] = MarkAppearance(false, round);
            appearances.Elements["/Yes"] = MarkAppearance(true, round);
            SetNormalAppearances(field, appearances);
            var characteristics = new PdfDictionary(_document);
            characteristics.Elements.SetString("/CA", round ? "l" : "8");
            field.Elements["/MK"] = characteristics;
            field.Elements.SetName("/V", filled ? "/Yes" : "/Off");
            field.Elements.SetName("/DV", filled ? "/Yes" : "/Off");
            field.Elements.SetName("/AS", filled ? "/Yes" : "/Off");
        }

        private void Damage(string label, int capacity, int superficial, int aggravated, bool crossOnly)
        {
            Ensure(75);
            _graphics.DrawString($"{label}: {capacity} caixas. " + (crossOnly ? "Vazia ou X." : "Escolha vazia, cheia ou X."), _font, XBrushes.Black, Left, _y + 12);
            _y += 20;
            for (var i = 0; i < capacity; i++)
            {
                var mark = i < aggravated ? "X" : i < aggravated + superficial ? "cheia" : "";
                var field = Widget($"{label} / caixa {i + 1}", Left + i * 25, _y, 23, 24, "/Ch");
                field.Elements.SetInteger("/Ff", 131072);
                var choices = new PdfArray(_document);
                foreach (var option in crossOnly ? new[] { "", "X" } : new[] { "", "cheia", "X" }) choices.Elements.Add(new PdfString(option));
                field.Elements["/Opt"] = choices;
                field.Elements.SetString("/V", mark);
                field.Elements.SetString("/DV", mark);
                SetAppearance(field, Appearance(23, 24, g =>
                {
                    g.DrawRectangle(XPens.Gray, 1, 1, 21, 22);
                    if (mark == "X") g.DrawString("X", _font, XBrushes.Black, 7, 16);
                    else if (mark == "cheia") g.DrawRectangle(XBrushes.Black, 4, 4, 15, 16);
                }));
            }
            _y += 38;
        }

        private PdfDictionary Widget(string title, double x, double y, double width, double height, string type)
        {
            _cancellation.ThrowIfCancellationRequested();
            var field = new PdfDictionary(_document);
            field.Elements.SetName("/Type", "/Annot");
            field.Elements.SetName("/Subtype", "/Widget");
            field.Elements.SetName("/FT", type);
            field.Elements.SetString("/T", $"vcs-{++_id}");
            field.Elements.SetString("/TU", title);
            field.Elements.SetString("/DA", "/Helv 10 Tf 0 g");
            field.Elements.SetInteger("/F", 4);
            field.Elements["/Rect"] = new PdfRectangle(new XRect(x, _page.Height.Point - y - height, width, height));
            field.Elements["/P"] = _page.Reference!;
            _document.Internals.AddObject(field);
            _fields.Elements.Add(field.Reference!);
            var annotations = _page.Elements.GetArray("/Annots");
            if (annotations is null) _page.Elements["/Annots"] = annotations = new PdfArray(_document);
            annotations.Elements.Add(field.Reference!);
            return field;
        }

        private PdfReference MarkAppearance(bool filled, bool round) => Appearance(16, 16, g =>
        {
            if (round)
            {
                g.DrawEllipse(XPens.Black, 1, 1, 14, 14);
                if (filled) g.DrawEllipse(XBrushes.Black, 4, 4, 8, 8);
            }
            else
            {
                g.DrawRectangle(XPens.Black, 1, 1, 14, 14);
                if (filled) g.DrawString("X", _font, XBrushes.Black, 4, 12);
            }
        });

        private PdfReference Appearance(double width, double height, Action<XGraphics> draw)
        {
            // Render through PDFsharp to embed Unicode fonts and keep appearances visible in non-form viewers.
            var page = _document.AddPage();
            page.Width = XUnit.FromPoint(width);
            page.Height = XUnit.FromPoint(height);
            using (var graphics = XGraphics.FromPdfPage(page)) draw(graphics);
            var appearance = new PdfDictionary(_document);
            appearance.Elements.SetName("/Type", "/XObject");
            appearance.Elements.SetName("/Subtype", "/Form");
            appearance.Elements["/BBox"] = new PdfRectangle(new XRect(0, 0, width, height));
            appearance.Elements["/Resources"] = page.Elements["/Resources"];
            appearance.CreateStream(page.Contents.CreateSingleContent().Stream.Value);
            _document.Internals.AddObject(appearance);
            _document.Pages.Remove(page);
            return appearance.Reference!;
        }
        private void SetAppearance(PdfDictionary field, PdfReference appearance)
        {
            var container = new PdfDictionary(_document);
            container.Elements["/N"] = appearance;
            field.Elements["/AP"] = container;
        }
        private void SetNormalAppearances(PdfDictionary field, PdfDictionary normal)
        {
            var container = new PdfDictionary(_document);
            container.Elements["/N"] = normal;
            field.Elements["/AP"] = container;
        }
        public void Save(string path) { _graphics.Dispose(); _document.Save(path); }
        public void Dispose() { _graphics.Dispose(); _document.Dispose(); }
    }
}
