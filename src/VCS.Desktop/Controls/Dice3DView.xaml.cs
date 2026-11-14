using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace VCS.Desktop.Controls;

/// <summary>Animated, theme-aware low-poly ten-sided die used to display V5 roll results.</summary>
public partial class Dice3DView : UserControl
{
    private static readonly MeshGeometry3D D10Mesh = CreateD10Mesh();
    private readonly AxisAngleRotation3D _rotation = new(new Vector3D(1, 1, 0), 0);

    public Dice3DView()
    {
        InitializeComponent();
        var material = (Material)DieViewport.FindResource("DieMaterial");
        var die = new GeometryModel3D(D10Mesh, material) { BackMaterial = material };
        var scene = new Model3DGroup();
        scene.Children.Add(new AmbientLight(Colors.White));
        scene.Children.Add(new DirectionalLight(Colors.White, new Vector3D(-1, -2, -3)));
        scene.Children.Add(die);
        DieViewport.Children.Add(new ModelVisual3D
        {
            Content = scene,
            Transform = new RotateTransform3D(_rotation)
        });
    }

    private void OnDieLoaded(object sender, RoutedEventArgs e)
    {
        var axis = new Vector3D(Random.Shared.NextDouble() + 0.2, Random.Shared.NextDouble() + 0.2,
            Random.Shared.NextDouble() + 0.2);
        axis.Normalize();
        _rotation.Axis = axis;
        var animation = new DoubleAnimation(0, Random.Shared.Next(720, 1261),
            new Duration(TimeSpan.FromMilliseconds(Random.Shared.Next(900, 1401))))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        _rotation.BeginAnimation(AxisAngleRotation3D.AngleProperty, animation);
    }

    private static MeshGeometry3D CreateD10Mesh()
    {
        var mesh = new MeshGeometry3D();
        var top = new Point3D(0, 1.12, 0);
        var bottom = new Point3D(0, -1.12, 0);
        var upper = new Point3D[5];
        var lower = new Point3D[5];

        for (var index = 0; index < 5; index++)
        {
            var upperAngle = Math.PI * 2 * index / 5;
            var lowerAngle = upperAngle + Math.PI / 5;
            upper[index] = new Point3D(Math.Cos(upperAngle) * 0.78, 0.34, Math.Sin(upperAngle) * 0.78);
            lower[index] = new Point3D(Math.Cos(lowerAngle) * 0.78, -0.34, Math.Sin(lowerAngle) * 0.78);
        }

        for (var index = 0; index < 5; index++)
        {
            var next = (index + 1) % 5;
            AddTriangle(mesh, top, upper[index], lower[index]);
            AddTriangle(mesh, top, lower[index], upper[next]);
            AddTriangle(mesh, bottom, lower[index], upper[next]);
            AddTriangle(mesh, bottom, upper[next], lower[next]);
        }

        if (mesh.CanFreeze) mesh.Freeze();
        return mesh;
    }

    private static void AddTriangle(MeshGeometry3D mesh, Point3D first, Point3D second, Point3D third)
    {
        var center = new Vector3D((first.X + second.X + third.X) / 3,
            (first.Y + second.Y + third.Y) / 3, (first.Z + second.Z + third.Z) / 3);
        var normal = Vector3D.CrossProduct(second - first, third - first);
        if (Vector3D.DotProduct(normal, center) < 0)
        {
            (second, third) = (third, second);
            normal = Vector3D.CrossProduct(second - first, third - first);
        }
        normal.Normalize();

        var start = mesh.Positions.Count;
        mesh.Positions.Add(first);
        mesh.Positions.Add(second);
        mesh.Positions.Add(third);
        mesh.Normals.Add(normal);
        mesh.Normals.Add(normal);
        mesh.Normals.Add(normal);
        mesh.TriangleIndices.Add(start);
        mesh.TriangleIndices.Add(start + 1);
        mesh.TriangleIndices.Add(start + 2);
    }
}
