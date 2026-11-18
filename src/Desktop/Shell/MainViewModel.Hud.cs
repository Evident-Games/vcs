using System.Collections.Specialized;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Game.Application.Characters;
using Game.Application.Chronicle;

namespace Game.Desktop.Shell;

public partial class MainViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHudCharacters))]
    private bool isHudChronicles;
    [ObservableProperty] private string hudNewChronicleName = "";
    [ObservableProperty] private string hudChronicleSearch = "";
    [ObservableProperty] private string hudSelectedChronicle = "";
    partial void OnHudChronicleSearchChanged(string value) => OnPropertyChanged(nameof(HudChronicles));
    partial void OnHudSelectedChronicleChanged(string value)
    {
        OnPropertyChanged(nameof(HudScenes));
        OnPropertyChanged(nameof(HasHudSelectedChronicle));
    }
    public bool HasHudSelectedChronicle => !string.IsNullOrWhiteSpace(HudSelectedChronicle);
    public IReadOnlyList<ChronicleEvent> HudScenes => string.IsNullOrWhiteSpace(HudSelectedChronicle) ? [] :
        (Narrator?.Events.Where(x => string.Equals(x.Chronicle.Trim(), HudSelectedChronicle, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(x.Kind, "Crônica", StringComparison.OrdinalIgnoreCase)).OrderByDescending(x => x.Date).ToArray() ?? []);
    private bool _hudInitialized;
    private string _hudAvatarLoadedPath = "";
    private BitmapSource? _hudAvatarCached;

    public bool IsHudCharacters => !IsHudChronicles;
    public string HudHeaderName => Editor?.Name ?? "Sua crônica";
    public string HudPlayerLabel => string.IsNullOrWhiteSpace(Display.HudPlayerName) ? "Jogador local" : Display.HudPlayerName.Trim();
    public string HudInitials => Initials(Editor?.Name ?? HudPlayerLabel);
    public string HudStatus => Editor is null ? "Nenhuma ficha ativa · modo local" : "Ficha ativa neste dispositivo";
    public bool HudHasActiveCharacter => Editor is not null;
    public IReadOnlyList<HudCharacterRow> HudCharacters => HudRoster(false);
    public IReadOnlyList<HudCharacterRow> HudNpcs => HudRoster(true);
    public IReadOnlyList<HudChronicleCard> HudChronicles => _all.Select(x => x.Chronicle)
        .Concat(Narrator?.Events.Select(x => x.Chronicle) ?? [])
        .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase).Where(x => x.Contains(HudChronicleSearch.Trim(), StringComparison.CurrentCultureIgnoreCase)).Order(StringComparer.CurrentCultureIgnoreCase)
        .Select(name => new HudChronicleCard(name,
            _all.Count(x => string.Equals(x.Chronicle.Trim(), name, StringComparison.OrdinalIgnoreCase)),
            Narrator?.Events.Count(x => string.Equals(x.Chronicle.Trim(), name, StringComparison.OrdinalIgnoreCase)) ?? 0)).ToArray();

    public BitmapSource? HudAvatarImage
    {
        get
        {
            if (_hudAvatarLoadedPath == Display.HudAvatarPath) return _hudAvatarCached;
            _hudAvatarLoadedPath = Display.HudAvatarPath;
            _hudAvatarCached = null;
            if (string.IsNullOrWhiteSpace(Display.HudAvatarPath) || !File.Exists(Display.HudAvatarPath)) return null;
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 104;
                image.UriSource = new Uri(Path.GetFullPath(Display.HudAvatarPath));
                image.EndInit(); image.Freeze();
                _hudAvatarCached = image;
                return _hudAvatarCached;
            }
            catch (Exception exception) when (exception is IOException or ArgumentException or NotSupportedException or UnauthorizedAccessException or FormatException)
            { return null; }
        }
    }

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2).Select(x => x[0])).ToUpperInvariant();

    private IReadOnlyList<HudCharacterRow> HudRoster(bool npc) => Characters.Where(x => x.IsNpc == npc)
        .Select(x => new HudCharacterRow(x, Initials(x.Name), OpenEditors.Any(e => e.Id == x.Id), Editor?.Id == x.Id)).ToArray();

    private async Task InitializeHudAsync()
    {
        if (_hudInitialized) return;
        if (Narrator is not null)
        {
            await Narrator.LoadAsync(_all);
            Narrator.Events.CollectionChanged += OnHudEventsChanged;
        }
        _hudInitialized = true;
        RefreshHud();
    }

    private void OnHudEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HudChronicles));
        OnPropertyChanged(nameof(HudScenes));
    }

    private void RefreshHud()
    {
        OnPropertyChanged(nameof(HudCharacters));
        OnPropertyChanged(nameof(HudNpcs));
        OnPropertyChanged(nameof(HudChronicles));
        RefreshHudProfile();
    }

    private void RefreshHudProfile()
    {
        OnPropertyChanged(nameof(HudHeaderName));
        OnPropertyChanged(nameof(HudPlayerLabel));
        OnPropertyChanged(nameof(HudInitials));
        OnPropertyChanged(nameof(HudStatus));
        OnPropertyChanged(nameof(HudHasActiveCharacter));
        OnPropertyChanged(nameof(HudAvatarImage));
    }

    [RelayCommand] private void SelectHudCharacters() => SelectHudSection(false);
    [RelayCommand] private void SelectHudChronicles() => SelectHudSection(true);

    private void SelectHudSection(bool chronicles)
    {
        var close = !Display.HudCollapsed && IsHudChronicles == chronicles;
        IsHudChronicles = chronicles;
        Display.HudCollapsed = close;
    }

    [RelayCommand]
    private void ShowCreateHudChronicle()
    {
        IsHudChronicles = true;
        Display.ShowSidebar = true;
        Display.HudCollapsed = false;
    }

    [RelayCommand] private void SelectHudChronicle(HudChronicleCard? card)
    {
        if (card is not null) HudSelectedChronicle = card.Name;
    }

    [RelayCommand] private async Task OpenHudSceneAsync(ChronicleEvent? scene)
    {
        if (scene is null || Narrator is null) return;
        if (!IsNarratorOpen) await ToggleNarratorAsync();
        if (!IsNarratorOpen) return;
        Narrator.ChronicleScope = scene.Chronicle;
        Narrator.SelectedEvent = scene;
        Display.HudCollapsed = true;
    }

    [RelayCommand] private async Task NewHudSceneAsync()
    {
        if (Narrator is null || string.IsNullOrWhiteSpace(HudSelectedChronicle)) return;
        if (!IsNarratorOpen) await ToggleNarratorAsync();
        if (!IsNarratorOpen) return;
        Narrator.ChronicleScope = HudSelectedChronicle;
        Narrator.NewEventCommand.Execute(null);
        Display.HudCollapsed = true;
    }

    [RelayCommand]
    private async Task OpenHudChronicleAsync(HudChronicleCard? card)
    {
        if (card is null || Narrator is null) return;
        if (!IsNarratorOpen) await ToggleNarratorAsync();
        if (IsNarratorOpen)
        {
            Narrator.ChronicleScope = card.Name;
            Display.HudCollapsed = true;
        }
    }

    [RelayCommand]
    private Task CreateHudChronicleAsync() => RunAsync(async () =>
    {
        if (Narrator is null) return;
        var name = HudNewChronicleName.Trim();
        if (name.Length is < 1 or > 120) { Error = "Informe um nome de crônica com até 120 caracteres."; return; }
        if (!HudChronicles.Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            await Narrator.RegisterChronicleAsync(name);
        HudNewChronicleName = "";
        HudChronicleSearch = "";
        HudSelectedChronicle = name;
        RefreshHud();
        Message = $"Crônica '{name}' disponível no HUD. Abra o card para organizar seus eventos.";
    });
}

public sealed record HudCharacterRow(CharacterSummary Character, string Initials, bool IsOpen, bool IsActive)
{
    public string Status => IsOpen ? "Ficha aberta neste dispositivo" : "Ficha salva no dispositivo";
}

public sealed record HudChronicleCard(string Name, int Characters, int Events)
{
    public string Description => $"{Characters} personagem(ns) · {Events} registro(s) da crônica";
}
