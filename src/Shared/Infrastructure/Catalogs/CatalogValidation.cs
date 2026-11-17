namespace VCS.Infrastructure.Catalogs;

/// <summary>Checks shared by the editable catalogs; failures are shown to the user as is.</summary>
internal static class CatalogValidation
{
    public static void Text(string label, string? value, int max, bool required = true)
    {
        if (value is null || required && string.IsNullOrWhiteSpace(value) || value.Length > max)
            throw new InvalidOperationException($"{label}: informe {(required ? "de 1 a" : "ate")} {max} caracteres.");
    }

    public static void Range(string label, int value, int min, int max)
    {
        if (value < min || value > max) throw new InvalidOperationException($"{label}: use um valor de {min} a {max}.");
    }

    public static InvalidOperationException BuiltInDelete() =>
        new("Itens que vem com o aplicativo nao podem ser excluidos, pois fichas podem usa-los. Use Restaurar padrao.");
}
