using System.Windows.Controls;
using System.Windows.Data;
using Game.Desktop.Sheet;

namespace Game.Desktop.Catalogs;

public partial class DisciplineCatalogView : UserControl
{
    public DisciplineCatalogView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => FillDisciplineGrid();
    }

    /// <summary>One grid for catalog disciplines followed by personal lines; CompositeCollection forwards changes item by item.</summary>
    private void FillDisciplineGrid() => DisciplineGrid.ItemsSource = DataContext is CharacterEditorViewModel editor
        ? new CompositeCollection
        {
            new CollectionContainer { Collection = editor.DisciplineItems },
            new CollectionContainer { Collection = editor.PersonalDisciplines.Lines }
        }
        : null;
}
