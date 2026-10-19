using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Views;

public partial class SheetSectionView : UserControl
{
    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(nameof(IsCompact),
        typeof(bool), typeof(SheetSectionView), new PropertyMetadata(false));

    public SheetSectionView() => InitializeComponent();

    /// <summary>Hides the card chrome and title so a bento tile can host the section.</summary>
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
}
