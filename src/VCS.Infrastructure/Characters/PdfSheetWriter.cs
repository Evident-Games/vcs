using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using VCS.Application.Catalogs;
using VCS.Application.Characters;
using VCS.Domain.Skills;

namespace VCS.Infrastructure.Characters;

/// <summary>Writes a fillable PDF sheet: page layout, text, ratings and form fields.</summary>
internal sealed partial class PdfSheetWriter : IDisposable
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

    public PdfSheetWriter(CharacterDto character, IDisciplineCatalog powers, IReadOnlyList<SkillDefinition> skills, CancellationToken cancellation)
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
