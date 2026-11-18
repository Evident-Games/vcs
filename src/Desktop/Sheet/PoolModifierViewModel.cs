using CommunityToolkit.Mvvm.ComponentModel;

namespace Game.Desktop.Sheet;

public partial class PoolModifierViewModel : ObservableObject
{
    [ObservableProperty] private string source = "Decisão do Narrador";
    [ObservableProperty] private string reason = "";
    [ObservableProperty] private int dice;
    [ObservableProperty] private bool isApplied = true;
}
