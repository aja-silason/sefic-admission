using Partners.Domain.Partners;

namespace Partners.UnitTests;

public sealed class PartnerTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    private static PartnerDetails Details(string name = "Ana Silva") =>
        PartnerDetails.Create(name, "123456789", "BOTH", null, 0m);

    [Fact]
    public void Create_starts_active_with_server_timestamps()
    {
        var partner = Partner.Create(Details(), T0);

        Assert.NotEqual(Guid.Empty, partner.Id);
        Assert.True(partner.IsActive);
        Assert.Equal(T0, partner.CreatedAt);
        Assert.Equal(T0, partner.UpdatedAt);
    }

    [Fact]
    public void Update_replaces_data_but_keeps_id_and_created_at()
    {
        var partner = Partner.Create(Details(), T0);
        var id = partner.Id;

        partner.Update(Details("Ana Maria"), isActive: false, T0.AddHours(1));

        Assert.Equal(id, partner.Id);
        Assert.Equal("Ana Maria", partner.Name);
        Assert.False(partner.IsActive);
        Assert.Equal(T0, partner.CreatedAt);
        Assert.Equal(T0.AddHours(1), partner.UpdatedAt);
    }

    [Fact]
    public void Timestamps_must_be_utc() =>
        Assert.Throws<ArgumentException>(() => Partner.Create(Details(), DateTime.Now));
}
