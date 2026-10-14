using Partners.Application.Partners;

namespace Partners.UnitTests;

public sealed class VersionTokenTests
{
    [Fact]
    public void Round_trips_an_eight_byte_rowversion()
    {
        byte[] version = [0, 0, 0, 0, 0, 0, 0x07, 0xD1];
        var token = VersionToken.Encode(version);

        Assert.True(VersionToken.TryDecode(token, out var decoded));
        Assert.Equal(version, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!")]
    [InlineData("AAAA")]              // valid Base64, but 3 bytes
    [InlineData("AAAAAAAAAAAAAA==")]  // valid Base64, but 10 bytes
    public void Rejects_malformed_tokens(string? token) =>
        Assert.False(VersionToken.TryDecode(token, out _));
}
