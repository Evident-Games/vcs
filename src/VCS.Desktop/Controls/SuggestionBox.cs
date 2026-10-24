using System.Collections;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VCS.Application.Disciplines;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Controls;

/// <summary>Paper line text box with a filtered drop-down of suggestions; any free text is still accepted.</summary>
public sealed class SuggestionBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string),
        typeof(SuggestionBox), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));
    public static readonly DependencyProperty SuggestionsProperty = DependencyProperty.Register(nameof(Suggestions), typeof(IEnumerable),
        typeof(SuggestionBox), new PropertyMetadata(null));
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(nameof(Placeholder), typeof(string),
        typeof(SuggestionBox), new PropertyMetadata("", (d, e) => ((SuggestionBox)d)._box.Tag = e.NewValue));
    public static readonly DependencyProperty EditorNameProperty = DependencyProperty.Register(nameof(EditorName), typeof(string),
        typeof(SuggestionBox), new PropertyMetadata("", (d, e) =>
        {
            var box = (SuggestionBox)d;
            AutomationProperties.SetName(box._box, (string)e.NewValue);
            AutomationProperties.SetName(box._list, $"Sugestoes de {e.NewValue}");
            AutomationProperties.SetName(box._toggle, $"Abrir lista de {e.NewValue}");
        }));

    private readonly TextBox _box;
    private readonly Popup _popup;
    private readonly ListBox _list;
    private readonly Button _toggle;
    private bool _picking;

    public SuggestionBox()
    {
        _box = new TextBox { MaxLength = 80, Padding = new(2, 1, 16, 1) };
        _box.SetResourceReference(StyleProperty, "PaperLineTextBox");
        _box.TextChanged += (_, _) =>
        {
            if (Text != _box.Text) Text = _box.Text;
            if (!_picking && _box.IsKeyboardFocusWithin) Open(filter: true);
        };
        _box.PreviewKeyDown += OnKey;
        _box.LostKeyboardFocus += (_, e) =>
        {
            if (e.NewFocus is DependencyObject target && IsInsidePopup(target)) return;
            _popup!.IsOpen = false;
        };

        _list = new ListBox { MaxHeight = 260, MinWidth = 220, BorderThickness = new(0), Focusable = false };
        _list.SetResourceReference(BackgroundProperty, "VcsCard");
        ScrollViewer.SetHorizontalScrollBarVisibility(_list, ScrollBarVisibility.Disabled);
        _list.ItemTemplate = ItemTemplate();
        _list.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (ItemsControl.ContainerFromElement(_list, (DependencyObject)e.OriginalSource) is ListBoxItem item)
                Pick(item.DataContext);
        };

        var frame = new Border { Child = _list, BorderThickness = new(1), CornerRadius = new(0, 0, 6, 6), Padding = new(2) };
        frame.SetResourceReference(Border.BorderBrushProperty, "VcsBorder");
        frame.SetResourceReference(Border.BackgroundProperty, "VcsCard");
        _popup = new Popup
        {
            Child = frame, PlacementTarget = _box, Placement = PlacementMode.Bottom, StaysOpen = false,
            AllowsTransparency = true, PopupAnimation = PopupAnimation.None
        };

        var chevron = new TextBlock { Text = "▾", FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
        chevron.SetResourceReference(TextBlock.ForegroundProperty, "VcsInputDescription");
        _toggle = new Button
        {
            Content = chevron, Focusable = false, Cursor = Cursors.Hand, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center, Margin = new(0, 0, 2, 0), ToolTip = "Ver opções do catálogo",
            Template = new ControlTemplate(typeof(Button)) { VisualTree = new FrameworkElementFactory(typeof(ContentPresenter)) }
        };
        _toggle.Click += (_, _) =>
        {
            if (_popup.IsOpen) { _popup.IsOpen = false; return; }
            _box.Focus();
            Open(filter: false);
        };

        var grid = new Grid();
        grid.Children.Add(_box);
        grid.Children.Add(_toggle);
        grid.Children.Add(_popup);
        Content = grid;
        Focusable = false;
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public IEnumerable? Suggestions { get => (IEnumerable?)GetValue(SuggestionsProperty); set => SetValue(SuggestionsProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public string EditorName { get => (string)GetValue(EditorNameProperty); set => SetValue(EditorNameProperty, value); }
    public bool IsDropDownOpen => _popup.IsOpen;
    internal TextBox Editor => _box;
    internal ListBox List => _list;

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (SuggestionBox)d;
        var text = (string?)e.NewValue ?? "";
        if (box._box.Text != text) box._box.Text = text;
    }

    public void Open(bool filter)
    {
        var term = filter ? CatalogText.Normalize(_box.Text) : "";
        var items = (Suggestions ?? Array.Empty<object>()).Cast<object>()
            .Where(x => term.Length == 0 || CatalogText.Normalize(Label(x)).Contains(term, StringComparison.Ordinal) ||
                x is SuggestionItem { HasNote: true } noted && CatalogText.Normalize(noted.Note).Contains(term, StringComparison.Ordinal))
            .ToArray();
        // A finished exact choice needs no list.
        if (filter && items.Length == 1 && CatalogText.Normalize(Label(items[0])) == term) items = [];
        _list.ItemsSource = items;
        _list.SelectedIndex = -1;
        _list.Width = Math.Max(ActualWidth, 220);
        _popup.IsOpen = items.Length > 0;
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (!_popup.IsOpen) Open(filter: false);
            else if (_list.Items.Count > 0) _list.SelectedIndex = Math.Min(_list.Items.Count - 1, _list.SelectedIndex + 1);
            _list.ScrollIntoView(_list.SelectedItem);
            e.Handled = true;
        }
        else if (e.Key == Key.Up && _popup.IsOpen)
        {
            _list.SelectedIndex = Math.Max(0, _list.SelectedIndex - 1);
            _list.ScrollIntoView(_list.SelectedItem);
            e.Handled = true;
        }
        else if (e.Key is Key.Enter or Key.Tab && _popup.IsOpen && _list.SelectedItem is { } item)
        {
            Pick(item);
            e.Handled = e.Key == Key.Enter;
        }
        else if (e.Key == Key.Escape && _popup.IsOpen)
        {
            _popup.IsOpen = false;
            e.Handled = true;
        }
    }

    public void Pick(object? item)
    {
        if (item is null) return;
        _picking = true;
        _box.Text = Label(item);
        _box.CaretIndex = _box.Text.Length;
        _picking = false;
        _popup.IsOpen = false;
    }

    private bool IsInsidePopup(DependencyObject target)
    {
        for (var node = target; node is not null; node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node))
            if (ReferenceEquals(node, _popup.Child)) return true;
        return false;
    }

    private static string Label(object item) => item is SuggestionItem suggestion ? suggestion.Value : item.ToString() ?? "";

    private static DataTemplate ItemTemplate()
    {
        var panel = new FrameworkElementFactory(typeof(DockPanel));
        var detail = new FrameworkElementFactory(typeof(TextBlock));
        detail.SetValue(DockPanel.DockProperty, Dock.Right);
        detail.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(SuggestionItem.Detail)));
        detail.SetValue(TextBlock.FontSizeProperty, 11.0);
        detail.SetValue(TextBlock.MarginProperty, new Thickness(12, 0, 0, 0));
        detail.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        detail.SetResourceReference(TextBlock.ForegroundProperty, "VcsAccent");
        var value = new FrameworkElementFactory(typeof(TextBlock));
        value.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(SuggestionItem.Value)));
        value.SetValue(TextBlock.FontSizeProperty, 12.5);
        value.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        var note = new FrameworkElementFactory(typeof(TextBlock));
        note.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(SuggestionItem.Note)) { FallbackValue = "" });
        note.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding(nameof(SuggestionItem.HasNote))
        {
            Converter = new BooleanToVisibilityConverter(), FallbackValue = Visibility.Collapsed
        });
        note.SetValue(TextBlock.FontSizeProperty, 10.5);
        note.SetValue(TextBlock.FontStyleProperty, FontStyles.Italic);
        note.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        note.SetResourceReference(TextBlock.ForegroundProperty, "VcsInputDescription");
        var text = new FrameworkElementFactory(typeof(StackPanel));
        text.AppendChild(value);
        text.AppendChild(note);
        panel.AppendChild(detail);
        panel.AppendChild(text);
        return new DataTemplate { VisualTree = panel };
    }
}
