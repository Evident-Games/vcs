using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace VCS.Desktop.Layout;

public partial class DisplaySettingsViewModel
{
    [ObservableProperty] private string workspaceBackgroundPath = "";
    [ObservableProperty] private BitmapSource? workspaceBackgroundImage;
    private readonly List<(BitmapSource Bitmap, TimeSpan Delay)> _backgroundFrames = [];
    private DispatcherTimer? _backgroundTimer;
    private int _backgroundFrameIndex;

    partial void OnWorkspaceBackgroundPathChanged(string value)
    {
        StopWorkspaceBackgroundAnimation();
        WorkspaceBackgroundImage = null;
        if (!string.IsNullOrWhiteSpace(value)) LoadWorkspaceBackground(value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void LoadWorkspaceBackground(string path)
    {
        try
        {
            var options = new DecoderOptions { TargetSize = new SixLabors.ImageSharp.Size(1280, 1280) };
            using var image = SixLabors.ImageSharp.Image.Load<Bgra32>(options, path);
            var format = image.Metadata.DecodedImageFormat?.Name;
            foreach (var frame in image.Frames)
            {
                var bytes = new byte[checked(frame.Width * frame.Height * 4)];
                frame.CopyPixelDataTo(bytes);
                var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96,
                    PixelFormats.Bgra32, null, bytes, frame.Width * 4);
                bitmap.Freeze();
                var delay = string.Equals(format, "GIF", StringComparison.OrdinalIgnoreCase)
                    ? (long)frame.Metadata.GetGifMetadata().FrameDelay * 10
                    : string.Equals(format, "WEBP", StringComparison.OrdinalIgnoreCase)
                        ? frame.Metadata.GetWebpMetadata().FrameDelay
                        : 100;
                _backgroundFrames.Add((bitmap, TimeSpan.FromMilliseconds(Math.Clamp(delay, 20, 60000))));
            }
            if (_backgroundFrames.Count == 0) return;
            WorkspaceBackgroundImage = _backgroundFrames[0].Bitmap;
            if (_backgroundFrames.Count == 1) return;
            _backgroundTimer ??= new DispatcherTimer(DispatcherPriority.Background);
            _backgroundTimer.Tick -= OnBackgroundFrameTick;
            _backgroundTimer.Tick += OnBackgroundFrameTick;
            _backgroundTimer.Interval = _backgroundFrames[0].Delay;
            _backgroundTimer.Start();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            StopWorkspaceBackgroundAnimation();
            WorkspaceBackgroundImage = null;
        }
    }

    private void OnBackgroundFrameTick(object? sender, EventArgs e)
    {
        if (_backgroundFrames.Count < 2 || _backgroundTimer is null) return;
        _backgroundFrameIndex = (_backgroundFrameIndex + 1) % _backgroundFrames.Count;
        var frame = _backgroundFrames[_backgroundFrameIndex];
        WorkspaceBackgroundImage = frame.Bitmap;
        _backgroundTimer.Interval = frame.Delay;
    }

    public void StopWorkspaceBackgroundAnimation()
    {
        if (_backgroundTimer is not null)
        {
            _backgroundTimer.Stop();
            _backgroundTimer.Tick -= OnBackgroundFrameTick;
        }
        _backgroundFrames.Clear();
        _backgroundFrameIndex = 0;
    }
}
