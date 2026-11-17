using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace VCS.Desktop.Layout;

public partial class DisplaySettingsViewModel
{
    [ObservableProperty] private string workspaceBackgroundPath = "";
    [ObservableProperty] private BitmapSource? workspaceBackgroundImage;
    [ObservableProperty] private bool? workspaceBackgroundIsDark;

    partial void OnWorkspaceBackgroundImageChanged(BitmapSource? value) =>
        WorkspaceBackgroundIsDark = value is null ? null : WorkspaceBackgroundContrast.IsDark(value);
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
            using var codec = SKCodec.Create(path);
            if (codec is null) return;
            var source = codec.Info;
            var info = new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            var frameInfo = codec.FrameInfo;
            var count = Math.Max(1, codec.FrameCount);
            for (var index = 0; index < count; index++)
            {
                using var decoded = new SKBitmap(info);
                var result = codec.GetPixels(info, decoded.GetPixels(), decoded.RowBytes, new SKCodecOptions(index, -1));
                if (result != SKCodecResult.Success) throw new System.IO.InvalidDataException("Não foi possível decodificar a imagem.");
                var bytes = new byte[checked(decoded.RowBytes * info.Height)];
                Marshal.Copy(decoded.GetPixels(), bytes, 0, bytes.Length);
                var bitmap = BitmapSource.Create(info.Width, info.Height, 96, 96,
                    PixelFormats.Pbgra32, null, bytes, decoded.RowBytes);
                bitmap.Freeze();
                var delay = index < frameInfo.Length ? frameInfo[index].Duration : 100;
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
