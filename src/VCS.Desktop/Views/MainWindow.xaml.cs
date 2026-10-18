using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using VCS.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace VCS.Desktop.Views;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;
    private bool _approved;
    private bool _checkingClose;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        Closing += OnClosing;
    }

    private void OnNewSheetMenu(object sender, RoutedEventArgs e)
    {
        var button = (System.Windows.Controls.Button)sender;
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_approved) return;
        e.Cancel = true;
        if (_checkingClose) return;
        _checkingClose = true;
        try
        {
            if (await _viewModel.TryCloseAsync())
            {
                _approved = true;
                // Defer until the original Closing event has returned.
                _ = Dispatcher.BeginInvoke(Close);
            }
        }
        finally { _checkingClose = false; }
    }
}
