using CommunityToolkit.Mvvm.ComponentModel;

namespace VCS.Desktop.ViewModels;

public partial class SheetFieldViewModel(string key, string label, string value, bool multiline = false) : ObservableObject
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public bool Multiline { get; } = multiline;
    public int MaximumLength => Multiline ? 20000 : 500;
    public double FieldHeight => Multiline ? 100 : 34;
    [ObservableProperty] private string value = value;
}
