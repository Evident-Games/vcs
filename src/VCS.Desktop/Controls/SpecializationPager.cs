using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using VCS.Domain.Characters;

namespace VCS.Desktop.Controls;

/// <summary>Edits one specialization per line, showing two lines per page with + and ‹ › controls.</summary>
public sealed class SpecializationPager : UserControl
{
    public const int PageSize = 2;

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
        typeof(SpecializationPager), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((SpecializationPager)d).OnTextChanged()));
    public static readonly DependencyProperty LockedLineProperty = DependencyProperty.Register(nameof(LockedLine), typeof(string),
        typeof(SpecializationPager), new PropertyMetadata("", (d, _) => ((SpecializationPager)d).Render()));
    public static readonly DependencyProperty EditorNameProperty = DependencyProperty.Register(nameof(EditorName), typeof(string),
        typeof(SpecializationPager), new PropertyMetadata("Especializacao", (d, _) => ((SpecializationPager)d).Render()));
    public static readonly DependencyProperty CountNameProperty = DependencyProperty.Register(nameof(CountName), typeof(string),
        typeof(SpecializationPager), new PropertyMetadata("Quantidade de especializacoes", (d, _) => ((SpecializationPager)d).Render()));

    private readonly List<string> _lines = [""];
    private readonly StackPanel _editors = new();
    private readonly TextBlock _count = new() { FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new(3, 0, 3, 0) };
    private readonly Button _previous, _next, _add;
    private int _page;
    private bool _pushing, _rendering;

    public SpecializationPager()
    {
        _previous = MiniButton("‹", "Especializacoes anteriores", () => GoTo(_page - 1));
        _next = MiniButton("›", "Proximas especializacoes", () => GoTo(_page + 1));
        _add = MiniButton("+", "Adicionar especializacao", AddLine);
        _count.SetResourceReference(TextBlock.ForegroundProperty, "VcsInputDescription");
        var side = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Margin = new(6, 4, 0, 0) };
        side.Children.Add(_previous);
        side.Children.Add(_count);
        side.Children.Add(_next);
        side.Children.Add(_add);
        var root = new DockPanel();
        DockPanel.SetDock(side, Dock.Right);
        root.Children.Add(side);
        root.Children.Add(_editors);
        Content = root;
        Render();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    /// <summary>A line granted by another rule (the predator) that is shown but cannot be edited here.</summary>
    public string LockedLine { get => (string)GetValue(LockedLineProperty); set => SetValue(LockedLineProperty, value); }
    public string EditorName { get => (string)GetValue(EditorNameProperty); set => SetValue(EditorNameProperty, value); }
    public string CountName { get => (string)GetValue(CountNameProperty); set => SetValue(CountNameProperty, value); }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(_lines.Count / (double)PageSize));
    public int Page => _page;

    private static string[] Split(string? text) =>
        (text ?? "").Split(['\r', '\n', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private void OnTextChanged()
    {
        if (_pushing) return;
        var incoming = Split(Text);
        if (incoming.SequenceEqual(_lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))) return;
        _lines.Clear();
        _lines.AddRange(incoming);
        if (_lines.Count == 0) _lines.Add("");
        _page = Math.Min(_page, PageCount - 1);
        Render();
    }

    private void Push()
    {
        _pushing = true;
        try { SetCurrentValue(TextProperty, string.Join("\n", _lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))); }
        finally { _pushing = false; }
        RefreshSide();
    }

    private void AddLine()
    {
        if (_lines.Count > 0 && string.IsNullOrWhiteSpace(_lines[^1]))
        {
            GoTo(PageCount - 1);
        }
        else
        {
            _lines.Add("");
            _page = PageCount - 1;
            Render();
        }
        var last = _editors.Children.OfType<TextBox>().LastOrDefault();
        last?.Dispatcher.BeginInvoke(() => Keyboard.Focus(last), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void GoTo(int page)
    {
        _page = Math.Clamp(page, 0, PageCount - 1);
        Render();
    }

    private void Render()
    {
        if (_editors is null) return;
        _rendering = true;
        try
        {
            _editors.Children.Clear();
            for (var index = _page * PageSize; index < Math.Min(_lines.Count, (_page + 1) * PageSize); index++)
                _editors.Children.Add(Editor(index));
        }
        finally { _rendering = false; }
        RefreshSide();
    }

    private TextBox Editor(int index)
    {
        var locked = !string.IsNullOrWhiteSpace(LockedLine) && ClanCatalog.SameName(_lines[index].Trim(), LockedLine);
        var box = new TextBox
        {
            Text = _lines[index], MaxLength = 120, MinHeight = 22, FontSize = 12, Opacity = 0.85,
            Tag = "+ especialização", IsReadOnly = locked,
            ToolTip = locked ? "Especialização do predador (troque o predador para remover)." : "Enter cria outra especialização."
        };
        box.SetResourceReference(StyleProperty, "PaperLineTextBox");
        if (locked) box.SetResourceReference(ForegroundProperty, "VcsAccent");
        AutomationProperties.SetName(box, index == 0 ? EditorName : $"{EditorName} {index + 1}");
        box.TextChanged += (_, _) =>
        {
            if (index >= _lines.Count || _lines[index] == box.Text) return;
            _lines[index] = box.Text;
            Push();
        };
        box.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (!string.IsNullOrWhiteSpace(box.Text)) AddLine();
        };
        box.LostKeyboardFocus += (_, e) =>
        {
            if (_rendering || !_editors.Children.Contains(box) || e.NewFocus is DependencyObject next && IsAncestorOf(next)) return;
            var before = _lines.Count;
            _lines.RemoveAll(string.IsNullOrWhiteSpace);
            if (_lines.Count == 0) _lines.Add("");
            if (_lines.Count == before) return;
            _page = Math.Min(_page, PageCount - 1);
            Dispatcher.BeginInvoke(Render, System.Windows.Threading.DispatcherPriority.Background);
        };
        return box;
    }

    private void RefreshSide()
    {
        var count = _lines.Count(x => !string.IsNullOrWhiteSpace(x));
        _count.Text = $"{count} esp.";
        _count.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
        _count.ToolTip = PageCount > 1 ? $"Página {_page + 1} de {PageCount}" : null;
        AutomationProperties.SetName(_count, CountName);
        AutomationProperties.SetName(_add, $"Adicionar {EditorName.ToLowerInvariant()}");
        var paged = _lines.Count > PageSize;
        _previous.Visibility = _next.Visibility = paged ? Visibility.Visible : Visibility.Collapsed;
        _previous.IsEnabled = _page > 0;
        _next.IsEnabled = _page < PageCount - 1;
        _add.Visibility = count == 0 || string.IsNullOrWhiteSpace(_lines[^1]) ? Visibility.Collapsed : Visibility.Visible;
    }

    private static Button MiniButton(string glyph, string name, Action click)
    {
        var face = new Border { Width = 14, Height = 14, CornerRadius = new(7), BorderThickness = new(1) };
        face.SetResourceReference(Border.BorderBrushProperty, "VcsBorder");
        face.SetResourceReference(Border.BackgroundProperty, "VcsInput");
        var text = new TextBlock
        {
            Text = glyph, FontSize = 10, LineHeight = 10, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "VcsAccent");
        face.Child = text;
        var template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) };
        var button = new Button { Content = face, Template = template, Cursor = Cursors.Hand, Focusable = false, ToolTip = name, VerticalAlignment = VerticalAlignment.Center };
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => click();
        button.IsEnabledChanged += (_, _) => face.Opacity = button.IsEnabled ? 1 : 0.35;
        button.MouseEnter += (_, _) => face.SetResourceReference(Border.BorderBrushProperty, "VcsAccent");
        button.MouseLeave += (_, _) => face.SetResourceReference(Border.BorderBrushProperty, "VcsBorder");
        return button;
    }
}
