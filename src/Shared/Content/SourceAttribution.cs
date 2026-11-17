using System.Reflection;

namespace VCS.Content;

/// <summary>Shared display helpers for content and rules citations.</summary>
public static class SourceAttribution
{
    public const string Pending = "Fonte pendente de conferência";
    public const string Custom = "Conteúdo personalizado — fonte definida pela mesa";

    public static string Format(string? source, bool isCustom = false) =>
        string.IsNullOrWhiteSpace(source) ? (isCustom ? Custom : Pending) : source.Trim();

    public static string ForRule(Type ruleType)
    {
        ArgumentNullException.ThrowIfNull(ruleType);
        var sources = ruleType.GetCustomAttributes<RuleSourceAttribute>()
            .Select(attribute => attribute.Citation).Distinct(StringComparer.Ordinal).ToArray();
        return sources.Length == 0 ? Pending : string.Join("; ", sources);
    }
}

/// <summary>Allows multiple sources, including an errata reference linked to a book.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RuleSourceAttribute(string source, string? page = null, string? errataFor = null) : Attribute
{
    public string Citation => $"{source}{(page is null ? "" : $", {page}")}" +
        (errataFor is null ? "" : $" (errata para {errataFor})");
}
