using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using VCS.Application.Abstractions;
using VCS.Application.Characters;
using VCS.Application.Disciplines;
using VCS.Desktop.Services;
using VCS.Desktop.ViewModels;
using VCS.Desktop.Views;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;

namespace VCS.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private Mutex? _instance;
    private bool _ownsMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var databasePath = Path.GetFullPath(Environment.GetEnvironmentVariable("VCS_DB_PATH") ?? DatabaseAccess.DefaultPath);
            var directory = Path.GetDirectoryName(databasePath)!;
            Directory.CreateDirectory(directory);
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(directory, "logs", "vcs-.log"), rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14)
                .CreateLogger();
            var instanceKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(databasePath.ToUpperInvariant())));
            _instance = new Mutex(false, @"Local\VCS-" + instanceKey);
            try { _ownsMutex = _instance.WaitOne(0); }
            catch (AbandonedMutexException)
            {
                _ownsMutex = true;
                Log.Warning("A instancia anterior foi encerrada inesperadamente");
            }
            if (!_ownsMutex)
            {
                MessageBox.Show("O VCS ja esta aberto para este banco de dados.", "VCS");
                Shutdown();
                return;
            }

            var builder = Host.CreateApplicationBuilder(e.Args);
            builder.Logging.ClearProviders();
            builder.Services.AddSerilog();
            builder.Services.AddSingleton(new DatabaseAccess(databasePath));
            builder.Services.AddSingleton<ICharacterRepository, SqliteCharacterRepository>();
            builder.Services.AddSingleton<IDatabaseMaintenance, SqliteDatabaseMaintenance>();
            builder.Services.AddSingleton<ICharacterFiles, JsonCharacterFiles>();
            builder.Services.AddSingleton<CharacterService>();
            builder.Services.AddSingleton<IDisciplineCatalog, ProvidedDisciplineCatalog>();
            builder.Services.AddSingleton<IAdvantageCatalog, SqliteAdvantageCatalog>();
            builder.Services.AddSingleton<ISkillCatalog, SqliteSkillCatalog>();
            builder.Services.AddSingleton<IPredatorCatalog, SqlitePredatorCatalog>();
            builder.Services.AddSingleton<ICharacterPdfExporter, CharacterPdfExporter>();
            builder.Services.AddSingleton<IDesktopDialogs, DesktopDialogs>();
            builder.Services.AddSingleton<ThemeService>();
            builder.Services.AddSingleton(new DisplaySettingsStore(Path.Combine(directory, "display-settings.json")));
            builder.Services.AddSingleton<Func<CharacterDto, CharacterEditorViewModel>>(provider => dto =>
                new(dto, provider.GetRequiredService<CharacterService>(),
                    provider.GetRequiredService<ILogger<CharacterEditorViewModel>>(), new DispatcherAutosaveScheduler(),
                    provider.GetRequiredService<IDisciplineCatalog>()));
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainWindow>();
            _host = builder.Build();
            await _host.StartAsync();
            await _host.Services.GetRequiredService<IDatabaseMaintenance>().InitializeAsync();
            var viewModel = _host.Services.GetRequiredService<MainViewModel>();
            MainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow.Show();
            await viewModel.InitializeAsync();
            Log.Information("VCS iniciado");
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Nao foi possivel iniciar o VCS");
            MessageBox.Show($"Nao foi possivel iniciar o VCS.\n{exception.Message}", "VCS", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        if (_ownsMutex) _instance?.ReleaseMutex();
        _instance?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
