using System.Windows;
using System.Windows.Controls;

namespace VCS.Desktop.Controls;

/// <summary>Packs children row by row into a grid that always fills the available area (no outer scrolling).</summary>
public sealed class BentoPanel : Panel
{
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(nameof(Columns), typeof(int),
        typeof(BentoPanel), new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty NarrowWidthProperty = DependencyProperty.Register(nameof(NarrowWidth), typeof(double),
        typeof(BentoPanel), new FrameworkPropertyMetadata(900d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(nameof(Gap), typeof(double),
        typeof(BentoPanel), new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty ColumnSpanProperty = DependencyProperty.RegisterAttached("ColumnSpan", typeof(int),
        typeof(BentoPanel), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static readonly DependencyProperty RowWeightProperty = DependencyProperty.RegisterAttached("RowWeight", typeof(double),
        typeof(BentoPanel), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
    public double NarrowWidth { get => (double)GetValue(NarrowWidthProperty); set => SetValue(NarrowWidthProperty, value); }
    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }
    public static int GetColumnSpan(DependencyObject element) => (int)element.GetValue(ColumnSpanProperty);
    public static void SetColumnSpan(DependencyObject element, int value) => element.SetValue(ColumnSpanProperty, value);
    public static double GetRowWeight(DependencyObject element) => (double)element.GetValue(RowWeightProperty);
    public static void SetRowWeight(DependencyObject element, double value) => element.SetValue(RowWeightProperty, value);

    private readonly List<Rect> _slots = [];

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size(double.IsInfinity(availableSize.Width) ? 1200 : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? 800 : availableSize.Height);
        Layout(size);
        for (var i = 0; i < InternalChildren.Count; i++) InternalChildren[i].Measure(_slots[i].Size);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Layout(finalSize);
        for (var i = 0; i < InternalChildren.Count; i++) InternalChildren[i].Arrange(_slots[i]);
        return finalSize;
    }

    private void Layout(Size size)
    {
        _slots.Clear();
        var columns = Math.Max(1, size.Width < NarrowWidth ? Math.Min(2, Columns) : Columns);
        var rows = new List<List<(int Index, int Column, int Span)>>();
        var column = columns;
        for (var i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i];
            if (child.Visibility == Visibility.Collapsed) continue;
            var span = Math.Clamp(GetColumnSpan(child), 1, columns);
            if (column + span > columns) { rows.Add([]); column = 0; }
            rows[^1].Add((i, column, span));
            column += span;
        }
        // The last item of each row stretches to close any gap left by hidden sections.
        foreach (var row in rows)
        {
            var last = row.Count - 1;
            row[last] = row[last] with { Span = columns - row[last].Column };
        }
        var weights = rows.Select(r => r.Select(x => GetRowWeight(InternalChildren[x.Index])).Max()).ToArray();
        var totalWeight = Math.Max(weights.Sum(), 0.0001);
        var usableHeight = Math.Max(0, size.Height - Gap * Math.Max(0, rows.Count - 1));
        var cellWidth = Math.Max(0, (size.Width - Gap * (columns - 1)) / columns);
        var slots = new Rect[InternalChildren.Count];
        var y = 0d;
        for (var r = 0; r < rows.Count; r++)
        {
            var height = usableHeight * weights[r] / totalWeight;
            foreach (var (index, col, span) in rows[r])
                slots[index] = new Rect(col * (cellWidth + Gap), y, cellWidth * span + Gap * (span - 1), height);
            y += height + Gap;
        }
        _slots.AddRange(slots);
    }
}
