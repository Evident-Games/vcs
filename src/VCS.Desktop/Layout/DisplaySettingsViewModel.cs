using CommunityToolkit.Mvvm.ComponentModel;

namespace VCS.Desktop.Layout;

/// <summary>
/// Display preferences: sheet mode (list, tabs or bento), sidebar and the PC and NPC layouts.
/// Theme choices live in DisplaySettingsViewModel.Themes.cs.
/// </summary>
public partial class DisplaySettingsViewModel : ObservableObject
{
    public DisplaySettingsViewModel()
    {
        Pc.Changed += OnLayoutChanged;
        Npc.Changed += OnLayoutChanged;
    }

    /// <summary>Any preference changed; the store saves on this.</summary>
    public event EventHandler? Changed;
    /// <summary>Section order, visibility, tabs or bento sizes changed.</summary>
    public event EventHandler? LayoutChanged;

    public SheetLayout Pc { get; } = SheetLayout.CreatePc();
    public SheetLayout Npc { get; } = SheetLayout.CreateNpc();
    public SheetLayout LayoutFor(bool npc) => npc ? Npc : Pc;

    /// <summary>Layout shown in the layout editor; opens on the active sheet's kind.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditedLayout), nameof(IsEditingPc))]
    private bool isEditingNpc;
    public bool IsEditingPc
    {
        get => !IsEditingNpc;
        set { if (value) IsEditingNpc = false; }
    }
    public SheetLayout EditedLayout => LayoutFor(IsEditingNpc);

    [ObservableProperty] private bool useTabs;
    [ObservableProperty] private bool useBento;
    [ObservableProperty] private bool showSidebar = true;
    public bool IsContinuous
    {
        get => !UseTabs && !UseBento;
        set
        {
            if (value) { UseTabs = false; UseBento = false; }
            OnPropertyChanged();
        }
    }

    partial void OnUseTabsChanged(bool value)
    {
        if (value) UseBento = false;
        OnPropertyChanged(nameof(IsContinuous));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    partial void OnUseBentoChanged(bool value)
    {
        if (value) UseTabs = false;
        OnPropertyChanged(nameof(IsContinuous));
        Changed?.Invoke(this, EventArgs.Empty);
    }

    partial void OnShowSidebarChanged(bool value) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnLayoutChanged(object? sender, EventArgs e)
    {
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
