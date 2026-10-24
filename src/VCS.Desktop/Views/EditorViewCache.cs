using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Views;

/// <summary>Keeps one built sheet view per open editor, so switching sheets reattaches it instead of rebuilding it.</summary>
public sealed class EditorViewCache : IValueConverter
{
    private readonly ConditionalWeakTable<CharacterEditorViewModel, CharacterEditorView> _views = [];

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is CharacterEditorViewModel editor ? _views.GetValue(editor, x => new CharacterEditorView { DataContext = x }) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
