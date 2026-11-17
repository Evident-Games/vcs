using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VCS.Desktop.Shell;

/// <summary>Main menu bar.</summary>
public partial class MainMenu : UserControl
{
    public MainMenu() => InitializeComponent();

    private void OnChooseBackground(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model) return;
        var picker = new OpenFileDialog
        {
            Title = "Escolher fundo da área de trabalho",
            Filter = "Imagens|*.webp;*.gif;*.png;*.jpg;*.jpeg;*.bmp|Todos os arquivos|*.*",
            CheckFileExists = true
        };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        var previous = model.Display.WorkspaceBackgroundPath;
        model.Display.WorkspaceBackgroundPath = picker.FileName;
        if (model.Display.WorkspaceBackgroundImage is not null) return;
        model.Display.WorkspaceBackgroundPath = previous;
        MessageBox.Show(Window.GetWindow(this), "Não foi possível abrir esta imagem.", "Fundo da área de trabalho", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnRemoveBackground(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel model) model.Display.WorkspaceBackgroundPath = "";
    }
}
