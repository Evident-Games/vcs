using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VCS.Desktop.Sheet;

public partial class BloodLineageDiagram : UserControl
{
    private const double FirstColumn = 132;
    private const double ColumnStep = 37.5;
    private const double BloodPotencyY = 16;
    private const double GenerationY = 68;
    private const double DotSize = 18;

    public static readonly DependencyProperty BloodPotencyProperty = DependencyProperty.Register(
        nameof(BloodPotency), typeof(int), typeof(BloodLineageDiagram),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty GenerationProperty = DependencyProperty.Register(
        nameof(Generation), typeof(int), typeof(BloodLineageDiagram),
        new FrameworkPropertyMetadata(13, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty BloodPotencyEnabledProperty = DependencyProperty.Register(
        nameof(BloodPotencyEnabled), typeof(bool), typeof(BloodLineageDiagram),
        new FrameworkPropertyMetadata(true, OnValueChanged));

    public int BloodPotency
    {
        get => (int)GetValue(BloodPotencyProperty);
        set => SetValue(BloodPotencyProperty, value);
    }

    public int Generation
    {
        get => (int)GetValue(GenerationProperty);
        set => SetValue(GenerationProperty, value);
    }

    public bool BloodPotencyEnabled
    {
        get => (bool)GetValue(BloodPotencyEnabledProperty);
        set => SetValue(BloodPotencyEnabledProperty, value);
    }

    public BloodLineageDiagram()
    {
        InitializeComponent();
        Loaded += (_, _) => RenderDiagram();
    }

    private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is BloodLineageDiagram diagram && diagram.IsLoaded)
            diagram.RenderDiagram();
    }

    private static double Column(int index) => FirstColumn + index * ColumnStep;

    private void RenderDiagram()
    {
        Diagram.Children.Clear();
        for (var column = 2; column <= 12; column++)
            DrawCell(Column(column), BloodPotencyY);
        for (var column = 0; column <= 12; column++)
            DrawCell(Column(column), GenerationY);
        DrawConnections();
        DrawLine((Column(1) + Column(2)) / 2, 10, (Column(1) + Column(2)) / 2, 89, false, "VcsBorder");
        DrawRowLabel("Blood Potency", 0, BloodPotencyY + 1, 118);
        DrawRowLabel("Generation", 0, GenerationY + 1, 118);

        var cross = new Button
        {
            Content = "×",
            Style = (Style)FindResource("LineageCrossButton"),
            Foreground = ResolveBrush("VcsAccentText"),
            Tag = 0,
            ToolTip = "Potencia do Sangue 0",
            IsEnabled = BloodPotencyEnabled
        };
        AutomationProperties.SetName(cross, "Potencia do Sangue 0");
        cross.Click += OnBloodPotencyClick;
        Canvas.SetLeft(cross, Column(2) - 10);
        Canvas.SetTop(cross, BloodPotencyY - 2);
        Diagram.Children.Add(cross);

        for (var potency = 1; potency <= 10; potency++)
        {
            var bloodDot = CreateDot(potency <= BloodPotency, $"Potencia do Sangue {potency}");
            bloodDot.IsEnabled = BloodPotencyEnabled;
            bloodDot.Tag = potency;
            bloodDot.Click += OnBloodPotencyClick;
            Canvas.SetLeft(bloodDot, Column(potency + 1) - DotSize / 2);
            Canvas.SetTop(bloodDot, BloodPotencyY);
            Diagram.Children.Add(bloodDot);
        }

        for (var index = 0; index < 13; index++)
        {
            var generation = 16 - index;
            var generationDot = CreateDot(generation == Generation, $"Geracao {generation}");
            generationDot.Tag = generation;
            generationDot.Click += OnGenerationClick;
            Canvas.SetLeft(generationDot, Column(index) - DotSize / 2);
            Canvas.SetTop(generationDot, GenerationY);
            Diagram.Children.Add(generationDot);
            DrawText($"{generation}.", Column(index) - 16, 91, 32, 15, "Georgia", 12, "VcsCardText", centered: true);
        }

        DrawGenerationBrackets();
        DrawText("(see page 214ff.)", 0, 232, 124, 16, "Georgia", 11, "VcsInputDescription", italic: true);
    }

    private void DrawConnections()
    {
        var bloodCenterY = BloodPotencyY + DotSize / 2;
        var generationCenterY = GenerationY + DotSize / 2;

        // Thin-blood generations 16–14 align with Blood Potency 0.
        DrawLine(Column(2), bloodCenterY + 10, Column(2), generationCenterY - 10, true);

        // Generations 13 and 12 can reach Blood Potency 3.
        DrawLine(Column(3), generationCenterY - 10, Column(5), bloodCenterY + 10, true);
        DrawLine(Column(4), generationCenterY - 10, Column(5), bloodCenterY + 10, true);

        // Generations 11 and 10 can reach Blood Potency 4.
        DrawLine(Column(5), generationCenterY - 10, Column(6), bloodCenterY + 10, true);
        DrawLine(Column(6), generationCenterY - 10, Column(6), bloodCenterY + 10, true);

        // Generations 9 through 4 align with Blood Potency 5 through 10.
        for (var index = 7; index <= 12; index++)
            DrawLine(Column(index), generationCenterY - 10, Column(index), bloodCenterY + 10, true);
    }

    private void DrawGenerationBrackets()
    {
        // Dotted reference stems under the generation labels; the last two use the theme accent.
        for (var index = 3; index <= 12; index++)
            DrawLine(Column(index), 109, Column(index), 129, true, index >= 11 ? "VcsAccentText" : "VcsInputDescription");

        // Thin-blood lineage bracket: 16th generation to the 13th.
        DrawLine(Column(0), 132, Column(0), 141);
        DrawLine(Column(0), 141, Column(3), 141);
        DrawLine(Column(3), 136, Column(3), 141);

        // A compact descending progeny tree, ending at the 3rd generation.
        DrawLine(Column(3), 133, Column(3), 151);
        DrawLine(Column(5), 133, Column(5), 151);
        DrawLine(Column(3), 151, Column(5), 151);
        DrawLine(Column(4), 151, Column(4), 156);

        JoinNextGeneration(6, 4, 161);
        JoinNextGeneration(7, 5, 170);
        JoinNextGeneration(8, 6, 179);
        JoinNextGeneration(9, 7, 188);
        JoinNextGeneration(10, 8, 197);
        JoinNextGeneration(11, 9, 206);
        JoinNextGeneration(12, 10, 215);

        DrawLine(Column(11), 215, Column(11), 222);
        DrawText("3.", Column(11) - 12, 222, 24, 16, "Georgia", 12, "VcsCardText", centered: true);
    }

    private void JoinNextGeneration(int generationColumn, int trunkColumn, double y)
    {
        DrawLine(Column(generationColumn), 133, Column(generationColumn), y);
        DrawLine(Column(trunkColumn), y, Column(generationColumn), y);
        var center = (Column(trunkColumn) + Column(generationColumn)) / 2;
        DrawLine(center, y, center, y + 5);
    }

    private void DrawCell(double centerX, double top)
    {
        var cell = new Rectangle
        {
            Width = 27,
            Height = 25,
            Opacity = 0.35,
            IsHitTestVisible = false
        };
        cell.SetResourceReference(Shape.FillProperty, "VcsInput");
        Canvas.SetLeft(cell, centerX - 13.5);
        Canvas.SetTop(cell, top - 3.5);
        Diagram.Children.Add(cell);
    }

    private Button CreateDot(bool selected, string name)
    {
        var button = new Button { Style = (Style)FindResource("LineageDotButton"), ToolTip = name };
        button.SetResourceReference(Control.BackgroundProperty, selected ? "VcsAccentText" : "VcsInput");
        button.SetResourceReference(Control.BorderBrushProperty, selected ? "VcsAccentBorder" : "VcsInputDescription");
        AutomationProperties.SetName(button, name);
        return button;
    }

    private void OnBloodPotencyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int value }) return;
        BloodPotency = value == BloodPotency ? Math.Max(0, value - 1) : value;
    }

    private void OnGenerationClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int value })
            Generation = value;
    }

    private void DrawRowLabel(string text, double x, double y, double width) =>
        DrawText(text, x, y, width, 22, "Segoe UI", 14, "VcsCardText");

    private void DrawText(string text, double x, double y, double width, double height,
        string fontFamily, double fontSize, string foregroundResource, bool centered = false, bool italic = false)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            Height = height,
            FontFamily = new FontFamily(fontFamily),
            FontSize = fontSize,
            FontStyle = italic ? FontStyles.Italic : FontStyles.Normal,
            TextAlignment = centered ? TextAlignment.Center : TextAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        label.SetResourceReference(TextBlock.ForegroundProperty, foregroundResource);
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        Diagram.Children.Add(label);
    }

    private void DrawLine(double x1, double y1, double x2, double y2, bool dotted = false,
        string strokeResource = "VcsInputDescription")
    {
        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            StrokeThickness = 1,
            IsHitTestVisible = false
        };
        if (dotted) line.StrokeDashArray = new DoubleCollection { 1, 2 };
        line.SetResourceReference(Shape.StrokeProperty, strokeResource);
        Diagram.Children.Add(line);
    }

    private Brush ResolveBrush(string resourceKey) =>
        TryFindResource(resourceKey) as Brush ?? SystemColors.ControlTextBrush;
}
