using CommunityToolkit.Mvvm.ComponentModel;

namespace VCS.Desktop.Sheet;

public partial class CharacterEditorViewModel
{
    [ObservableProperty] private double floatingX;
    [ObservableProperty] private double floatingY;
    [ObservableProperty] private double floatingWidth = 480;
    [ObservableProperty] private double floatingHeight = 480;
    [ObservableProperty] private string windowViewMode = "Default";
}
