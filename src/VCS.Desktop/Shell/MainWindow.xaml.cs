using System.ComponentModel;
using Wpf.Ui.Controls;

namespace VCS.Desktop.Shell;

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
