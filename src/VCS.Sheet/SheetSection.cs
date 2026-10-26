namespace VCS.Sheet;

/// <summary>
/// Keys of the player sheet sections. They tag experience costs and warnings, name the PDF sections and are
/// saved in display preferences, so the values must never change.
/// </summary>
public static class SheetSection
{
    public const string Identity = "Identity";
    public const string Attributes = "Attributes";
    public const string Skills = "Skills";
    public const string Disciplines = "Disciplines";
    public const string Advantages = "Advantages";
    public const string State = "State";
    public const string Background = "Background";
    public const string Notes = "Notes";
}
