using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VCS.Desktop.Sheet;

public partial class SheetSectionView : UserControl
{
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact),
        typeof(bool), typeof(SheetSectionView), new PropertyMetadata(false));

    public SheetSectionView()
    {
        InitializeComponent();
        // Build the section body after the first frame: opening a sheet paints its frame at once and the
        // sections fill in one by one, instead of blocking the UI on thousands of controls.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, ShowBody);
    }

    private void ShowBody()
    {
        if (DataContext is not EditorSection section) return;
        Body.ContentTemplate = (DataTemplate)Resources[section.Key];
        Body.Content = section.Editor;
    }

    /// <summary>Hides the card chrome and title so a bento tile can host the section.</summary>
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
}
