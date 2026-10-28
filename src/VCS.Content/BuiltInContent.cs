using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VCS.Content;

/// <summary>
/// Reads the content that ships with the app from the JSON files in Data (embedded in this assembly).
/// Only this class knows the file format.
/// </summary>
public static class BuiltInContent
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static IReadOnlyList<T> Load<T>(string file)
    {
        using var stream = typeof(BuiltInContent).Assembly.GetManifestResourceStream("VCS.Content.Data." + file)
            ?? throw new InvalidDataException($"O conteudo {file} nao foi incluido no aplicativo.");
        return Array.AsReadOnly(JsonSerializer.Deserialize<T[]>(stream, Options)
            ?? throw new InvalidDataException($"O conteudo {file} esta vazio."));
    }
}
