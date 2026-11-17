using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SixLabors.ImageSharp;

namespace VCS.Desktop.Layout;

public partial class DisplaySettingsViewModel
{
    [ObservableProperty] private string workspaceBackgroundPath = "";
    [ObservableProperty] private BitmapSource? workspaceBackgroundImage;

    partial void OnWorkspaceBackgroundPathChanged(string value)
    {
        WorkspaceBackgroundImage = LoadWorkspaceBackground(value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static BitmapSource? LoadWorkspaceBackground(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(path);
            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            stream.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }
}
