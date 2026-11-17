using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using VCS.Content.V5;

namespace VCS.Content;

/// <summary>
/// Reads the V5 content embedded in the V5 content assembly.
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
        using var stream = typeof(V5ContentResources).Assembly.GetManifestResourceStream("VCS.Content.Data." + file)
            ?? throw new InvalidDataException($"O conteudo {file} nao foi incluido no aplicativo.");
        return Array.AsReadOnly(JsonSerializer.Deserialize<T[]>(stream, Options)
            ?? throw new InvalidDataException($"O conteudo {file} esta vazio."));
    }
}
