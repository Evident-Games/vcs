using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using VCS.Application.Characters;
using VCS.Desktop.ViewModels;

namespace VCS.Desktop.Views;

public partial class HomeView : UserControl
{
    public HomeView() => InitializeComponent();

    private void OnCharacterMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2 ||
            sender is not ListBox list || e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(list, source) is not ListBoxItem { DataContext: CharacterSummary character } ||
            DataContext is not MainViewModel model)
            return;
        e.Handled = true;
        list.SelectedItem = character;
        model.OpenCharacterCommand.Execute(character);
    }
}
