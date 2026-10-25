using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VCS.Application.Catalogs;

public static class CatalogText
{
    public static string Normalize(string value) => string.Concat(value.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        .Normalize(NormalizationForm.FormC).Trim().ToUpperInvariant();

    public static string StableId(string prefix, string value) =>
        prefix + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(value))));
}
