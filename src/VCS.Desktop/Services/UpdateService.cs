using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace VCS.Desktop.Services;

public sealed record UpdateInfo(Version Version, string DownloadUrl, string? Sha256, string PageUrl);

public sealed class UpdateService
{
    // Newest release including pre-releases (/releases/latest skips them); drafts are hidden from anonymous calls.
    private const string LatestReleaseUrl = "https://api.github.com/repos/Evident-Games/vcs/releases?per_page=1";
    public const string AfterUpdateArgument = "--after-update";
    private static readonly HttpClient Http = CreateClient();

    public static Version CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);

    public static bool IsDisabled => Environment.GetEnvironmentVariable("VCS_DISABLE_UPDATE_CHECK") == "1";

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (IsDisabled) return null;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var json = await Http.GetStringAsync(LatestReleaseUrl, timeout.Token);
        return Parse(json, CurrentVersion);
    }

    public static UpdateInfo? Parse(string json, Version current)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.GetArrayLength() == 0) return null;
        var root = document.RootElement[0];
        var tag = root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var latest) || Normalize(latest) <= Normalize(current)) return null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString()?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) != true) continue;
            var digest = asset.TryGetProperty("digest", out var value) ? value.GetString() : null;
            return new UpdateInfo(latest, asset.GetProperty("browser_download_url").GetString()!,
                digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true ? digest[7..] : null,
                root.GetProperty("html_url").GetString()!);
        }
        return null;
    }

    // Returns false when the exe folder is not writable (installed copy); the caller opens the release page instead.
    public async Task<bool> ApplyAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Caminho do aplicativo desconhecido.");
        var downloaded = exe + ".new";
        try
        {
            await using (var target = File.Create(downloaded))
            await using (var source = await Http.GetStreamAsync(update.DownloadUrl, cancellationToken))
                await source.CopyToAsync(target, cancellationToken);
        }
        catch (UnauthorizedAccessException) { return false; }
        if (update.Sha256 is not null)
        {
            await using var stream = File.OpenRead(downloaded);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
            stream.Close();
            if (!hash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(downloaded);
                throw new InvalidDataException("O download da atualizacao esta corrompido. Tente novamente.");
            }
        }
        // Windows allows renaming a running exe, so the swap works without a helper process.
        File.Move(exe, exe + ".old", true);
        File.Move(downloaded, exe);
        Process.Start(new ProcessStartInfo(exe, AfterUpdateArgument) { UseShellExecute = false });
        return true;
    }

    public static void CleanupPreviousVersion()
    {
        try
        {
            if (Environment.ProcessPath is { } exe && File.Exists(exe + ".old")) File.Delete(exe + ".old");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static void OpenPage(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VCS/" + CurrentVersion);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }
}
