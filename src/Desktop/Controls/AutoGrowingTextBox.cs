using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VCS.Desktop.Controls;

public sealed class AutoGrowingTextBox : TextBox
{
    public AutoGrowingTextBox()
    {
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        TextChanged += (_, _) => QueueResize();
        SizeChanged += (_, _) => QueueResize();
    }

    private void QueueResize() =>
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ResizeToContent);

    private void ResizeToContent()
    {
        if (!IsLoaded || ActualWidth <= 0) return;
        var lines = Math.Max(1, LineCount);
        var lineHeight = Math.Max(FontSize * 1.25, FontFamily.LineSpacing * FontSize);
        var requiredHeight = Math.Ceiling(lines * lineHeight + Padding.Top + Padding.Bottom + 2);
        if (double.IsNaN(Height) || double.IsInfinity(Height) || Math.Abs(Height - requiredHeight) > 0.5)
            Height = Math.Max(MinHeight, requiredHeight);
    }
}
