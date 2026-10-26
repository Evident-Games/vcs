using System.Collections.ObjectModel;
using System.ComponentModel;
using VCS.Sheet;

namespace VCS.Desktop.Layout;

public sealed record LayoutTab(string Name, SectionOption[] Sections);

/// <summary>Section arrangement for one kind of sheet (PC or NPC), shared by every sheet of that kind.</summary>
public sealed class SheetLayout
{
    public const int BentoColumns = 4;
    private bool _reordering;

    public SheetLayout(string name, params SectionOption[] sections)
    {
        Name = name;
        Sections = sections;
        for (var i = 0; i < Sections.Length; i++)
        {
            Sections[i].SectionOrder = i + 1;
            Sections[i].TabOrder = Array.FindIndex(Sections, x => x.TabName == Sections[i].TabName) + 1;
            Sections[i].PropertyChanged += OnSectionChanged;
        }
        OrderedSections = new(Sections);
        RefreshTabs();
    }

    public static SheetLayout CreatePc() => new("Personagem",
        new(SheetSection.Identity, "Identidade", "Identidade", 2), new(SheetSection.Attributes, "Atributos", "Atributos", 2),
        new(SheetSection.Skills, "Habilidades", "Habilidades", 2, 2), new(SheetSection.Disciplines, "Disciplinas", "Disciplinas"),
        new(SheetSection.Advantages, "Vantagens e desvantagens", "Vantagens"), new(SheetSection.State, "Estado e sangue", "Estado"),
        new(SheetSection.Background, "Historia e conviccoes", "História"), new(SheetSection.Notes, "Anotacoes", "História", 2));

    public static SheetLayout CreateNpc() => new("NPC",
        new("NpcHeader", "Ficha de NPC", "Combate", 2), new("NpcCombat", "Combate", "Combate", 2, 2),
        new("NpcAttributes", "Atributos", "Atributos", 2), new("NpcSkills", "Habilidades", "Atributos", 2, 2),
        new("NpcDisciplines", "Disciplinas", "Disciplinas", 2), new("NpcAdvantages", "Vantagens e defeitos", "Vantagens", 2),
        new("NpcHistory", "Histórico", "Histórico", 4));

    public string Name { get; }
    public SectionOption[] Sections { get; }
    /// <summary>Sections in display order; the layout editor binds here so drag and drop can reorder it.</summary>
    public ObservableCollection<SectionOption> OrderedSections { get; }
    /// <summary>Tabs in display order, for the tab editor.</summary>
    public ObservableCollection<LayoutTab> Tabs { get; } = [];
    public event EventHandler? Changed;

    public SectionOption? Find(string key) => Sections.FirstOrDefault(x => x.Key == key);

    /// <summary>Moves a section to a new position; tab order follows the first section of each tab group.</summary>
    public void MoveSection(SectionOption section, int index)
    {
        var current = OrderedSections.IndexOf(section);
        index = Math.Clamp(index, 0, OrderedSections.Count - 1);
        if (current < 0 || current == index) return;
        OrderedSections.Move(current, index);
        Renumber();
    }

    /// <summary>Puts a section in a tab (new or existing) and keeps tab order consistent.</summary>
    public void MoveToTab(SectionOption section, string tab)
    {
        tab = tab.Trim();
        if (tab.Length == 0 || string.Equals(TabKey(section), tab, StringComparison.OrdinalIgnoreCase)) return;
        _reordering = true;
        try { section.TabName = tab; }
        finally { _reordering = false; }
        Renumber();
    }

    public void RenameTab(string from, string to)
    {
        to = to.Trim();
        if (to.Length == 0 || to.Length > 80) return;
        _reordering = true;
        try
        {
            foreach (var section in Sections.Where(x => string.Equals(TabKey(x), from, StringComparison.OrdinalIgnoreCase)))
                section.TabName = to;
        }
        finally { _reordering = false; }
        Renumber();
    }

    public string NewTabName()
    {
        var names = Sections.Select(TabKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = "Nova aba";
        for (var i = 2; names.Contains(name); i++) name = $"Nova aba {i}";
        return name;
    }

    public static string TabKey(SectionOption section)
    {
        var name = section.TabName.Trim();
        return name.Length > 0 ? name : section.DisplayTitle;
    }

    private void Renumber()
    {
        _reordering = true;
        try
        {
            var tabs = OrderedSections.Select(TabKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            for (var i = 0; i < OrderedSections.Count; i++)
            {
                OrderedSections[i].SectionOrder = i + 1;
                OrderedSections[i].TabOrder = tabs.FindIndex(x =>
                    string.Equals(x, TabKey(OrderedSections[i]), StringComparison.OrdinalIgnoreCase)) + 1;
            }
        }
        finally { _reordering = false; }
        RaiseChanged();
    }

    private void SyncOrderedSections()
    {
        var sorted = Sections.OrderBy(x => x.SectionOrder).ThenBy(x => Array.IndexOf(Sections, x)).ToArray();
        for (var i = 0; i < sorted.Length; i++)
        {
            var from = OrderedSections.IndexOf(sorted[i]);
            if (from != i) OrderedSections.Move(from, i);
        }
    }

    private void RefreshTabs()
    {
        Tabs.Clear();
        foreach (var group in OrderedSections.GroupBy(TabKey, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(g => g.Min(x => x.TabOrder)))
            Tabs.Add(new(group.Key, group.ToArray()));
    }

    private void RaiseChanged()
    {
        RefreshTabs();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnSectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_reordering) return;
        if (e.PropertyName == nameof(SectionOption.SectionOrder)) SyncOrderedSections();
        if (e.PropertyName is nameof(SectionOption.IsVisible) or nameof(SectionOption.TabName) or
            nameof(SectionOption.Caption) or nameof(SectionOption.SectionOrder) or nameof(SectionOption.TabOrder) or
            nameof(SectionOption.Span) or nameof(SectionOption.Height))
            RaiseChanged();
    }
}
