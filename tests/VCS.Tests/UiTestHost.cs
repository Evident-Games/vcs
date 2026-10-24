using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using VCS.Application.Characters;
using VCS.Desktop.Layout;
using VCS.Desktop.Sheet;
using VCS.Desktop.Shell;
using VCS.Desktop.Themes;
using VCS.Infrastructure.Catalogs;
using VCS.Infrastructure.Characters;
using VCS.Infrastructure.Persistence;

namespace VCS.Tests;

[CollectionDefinition(Name)]
public sealed class WpfCollection : ICollectionFixture<WpfApp>
{
    public const string Name = "WPF";
}

/// <summary>
/// WPF allows one Application per process, so every UI test shares it on one STA thread and runs there in turn.
/// </summary>
public sealed class WpfApp : IDisposable
{
    private readonly Thread _thread;
    private readonly Dispatcher _dispatcher;

    public WpfApp()
    {
        var ready = new TaskCompletionSource<Dispatcher>();
        _thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            _ = new System.Windows.Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
                Resources = new ResourceDictionary { Source = new Uri("/VCS.Desktop;component/Themes/Resources.xaml", UriKind.Relative) }
            };
            ready.SetResult(Dispatcher.CurrentDispatcher);
            Dispatcher.Run();
        });
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.IsBackground = true;
        _thread.Start();
        _dispatcher = ready.Task.Result;
    }

    /// <summary>Runs a test body against a fresh window, database and settings folder.</summary>
    public Task RunAsync(Func<UiSession, Task> body) => _dispatcher.InvokeAsync(async () =>
    {
        var session = await UiSession.StartAsync();
        try
        {
            await body(session);
            Assert.Empty(session.BindingErrors);
            await session.CloseAsync();
        }
        finally { session.Dispose(); }
    }).Task.Unwrap().WaitAsync(TimeSpan.FromSeconds(120));

    public void Dispose()
    {
        _dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
        _dispatcher.InvokeShutdown();
        _thread.Join(TimeSpan.FromSeconds(5));
    }
}

public sealed class UiSession : IDisposable
{
    private readonly BindingErrorListener _bindings = new();

    private UiSession(string directory) => Directory = directory;

    public string Directory { get; }
    public System.Windows.Application App => System.Windows.Application.Current;
    public MainWindow Window { get; private set; } = null!;
    public MainViewModel Model { get; private set; } = null!;
    public CharacterService Service { get; private set; } = null!;
    public TestDialogs Dialogs { get; private set; } = null!;
    public IReadOnlyList<string> BindingErrors => _bindings.Errors;

    public void Deconstruct(out System.Windows.Application app, out MainWindow window, out MainViewModel model,
        out CharacterService service, out TestDialogs dialogs, out string directory) =>
        (app, window, model, service, dialogs, directory) = (App, Window, Model, Service, Dialogs, Directory);

    public static async Task<UiSession> StartAsync()
    {
        var session = new UiSession(Path.Combine(Path.GetTempPath(), "VCS-ui-" + Guid.NewGuid().ToString("N")));
        PresentationTraceSources.DataBindingSource.Listeners.Add(session._bindings);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        var database = new DatabaseAccess(Path.Combine(session.Directory, "test.db"));
        await new SqliteDatabaseMaintenance(database).InitializeAsync();
        var service = new CharacterService(new SqliteCharacterRepository(database));
        session.Service = service;
        session.Dialogs = new TestDialogs { PdfPath = Path.Combine(session.Directory, "ui-npc.pdf") };
        session.Model = new MainViewModel(service, new JsonCharacterFiles(), new SqliteDatabaseMaintenance(database),
            session.Dialogs, new ThemeService(), dto => new(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                new DispatcherAutosaveScheduler(), new ProvidedDisciplineCatalog()),
            NullLogger<MainViewModel>.Instance, new DisplaySettingsStore(Path.Combine(session.Directory, "display.json")),
            new SqliteAdvantageCatalog(database), new SqliteSkillCatalog(database),
            new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(database)),
            new SqlitePredatorCatalog(database));
        session.Window = new MainWindow(session.Model);
        session.App.MainWindow = session.Window;
        session.Window.Show();
        await session.Model.InitializeAsync();
        return session;
    }

    /// <summary>Opens a new character sheet with the given changes applied and saved.</summary>
    public async Task<CharacterEditorViewModel> CreatePcAsync(Action<CharacterEditorViewModel>? edit = null)
    {
        await Model.CreateCommand.ExecuteAsync(null);
        var editor = Model.Editor!;
        edit?.Invoke(editor);
        Assert.True(await editor.FlushAsync());
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        return editor;
    }

    public async Task CloseAsync()
    {
        Assert.True(await Model.TryCloseAsync());
        var closed = false;
        Window.Closed += (_, _) => closed = true;
        Window.Close();
        var timeout = Stopwatch.StartNew();
        while (!closed && timeout.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(50);
        Assert.True(closed, "A janela nao fechou.");
    }

    public void Dispose()
    {
        PresentationTraceSources.DataBindingSource.Listeners.Remove(_bindings);
        if (Window?.IsVisible == true) Window.Hide();
        if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
    }

    private sealed class BindingErrorListener : TraceListener
    {
        public List<string> Errors { get; } = [];
        public override void Write(string? message) { if (message is not null) Errors.Add(message); }
        public override void WriteLine(string? message) { if (message is not null) Errors.Add(message); }
    }
}

public sealed class TestDialogs : VCS.Desktop.Shell.IDesktopDialogs
{
    public string PdfPath { get; init; } = "";
    public string? SavePdf() => PdfPath;
    public bool Confirm(string message) => false;
    public string? OpenJson() => null;
    public string? SaveJson() => null;
    public string? OpenBackup() => null;
    public string? SaveBackup() => null;
}
