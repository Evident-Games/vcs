using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Views;

/// <summary>One section of the NPC sheet; Part is the section key (see SheetLayout.CreateNpc).</summary>
public partial class NpcQuickSheetView : UserControl
{
    public static readonly DependencyProperty PartProperty = DependencyProperty.Register(nameof(Part), typeof(string),
        typeof(NpcQuickSheetView), new PropertyMetadata(null, (d, _) => ((NpcQuickSheetView)d).ShowPart()));

    public NpcQuickSheetView() => InitializeComponent();

    public string? Part { get => (string?)GetValue(PartProperty); set => SetValue(PartProperty, value); }

    private void ShowPart() => Body.ContentTemplate = Part is { } key ? (DataTemplate)Resources[key] : null;
}
