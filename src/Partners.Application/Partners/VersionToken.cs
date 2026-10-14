using System.Diagnostics.CodeAnalysis;

namespace Partners.Application.Partners;

/// <summary>
/// Opaque concurrency token exposed by the API: the SQL Server rowversion (8 bytes) encoded as Base64.
/// </summary>
public static class VersionToken
{
    private const int RowVersionLength = 8;

    public static string Encode(byte[] version) => Convert.ToBase64String(version);

    /// <summary>Accepts only canonical Base64 that decodes to exactly 8 bytes.</summary>
    public static bool TryDecode(string? token, [NotNullWhen(true)] out byte[]? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        Span<byte> buffer = stackalloc byte[RowVersionLength + 3];
        if (!Convert.TryFromBase64String(token, buffer, out var written) || written != RowVersionLength)
        {
            return false;
        }

        version = buffer[..RowVersionLength].ToArray();
        return true;
    }
}
