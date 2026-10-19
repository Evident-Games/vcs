using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Views;

public partial class CharacterEditorView : UserControl
{
    public static readonly DependencyProperty IsPaneProperty = DependencyProperty.Register(nameof(IsPane), typeof(bool),
        typeof(CharacterEditorView), new PropertyMetadata(false));
    public bool IsPane { get => (bool)GetValue(IsPaneProperty); set => SetValue(IsPaneProperty, value); }
    public CharacterEditorView() => InitializeComponent();
}