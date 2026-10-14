using Partners.Domain.Common;
using Partners.Domain.Partners;

namespace Partners.UnitTests;

public sealed class PartnerDetailsTests
{
    private static PartnerDetails Create(
        string? name = "Ana Silva",
        string? taxNumber = "123456789",
        string? type = "CUSTOMER",
        string? email = null,
        decimal? creditLimit = 100m) =>
        PartnerDetails.Create(name, taxNumber, type, email, creditLimit);

    private static IReadOnlyDictionary<string, string[]> Errors(Action act) =>
        Assert.Throws<DomainValidationException>(act).Errors;

    [Fact]
    public void Trims_name_and_normalizes_tax_number_keeping_leading_zeros()
    {
        var details = Create(name: "  Ana Silva  ", taxNumber: "001.234.567");

        Assert.Equal("Ana Silva", details.Name);
        Assert.Equal("001234567", details.TaxNumber.Value);
    }

    [Theory]
    [InlineData("001 234 567")]
    [InlineData("001-234-567")]
    [InlineData(" 0.0-1 234567 ")]
    public void Different_representations_of_a_tax_number_normalize_to_the_same_value(string raw) =>
        Assert.Equal("001234567", Create(taxNumber: raw).TaxNumber.Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678")]       // 8 digits
    [InlineData("1234567890")]     // 10 digits
    [InlineData("12345678A")]      // letter
    [InlineData("123_456_789")]    // separator not accepted
    [InlineData("12345678\t9")]    // tab is not a space
    [InlineData("١٢٣٤٥٦٧٨٩")]      // non-ASCII digits
    public void Rejects_invalid_tax_numbers(string? raw) =>
        Assert.Contains("TaxNumber", Errors(() => Create(taxNumber: raw)).Keys);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData(" A ")]
    public void Rejects_name_that_is_missing_or_too_short_after_trim(string? name) =>
        Assert.Contains("Name", Errors(() => Create(name: name)).Keys);

    [Fact]
    public void Name_length_limits_apply_after_trim()
    {
        Assert.Equal("Al", Create(name: " Al ").Name);
        Assert.Equal(100, Create(name: "  " + new string('x', 100) + "  ").Name.Length);
        Assert.Contains("Name", Errors(() => Create(name: new string('x', 101))).Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Blank_email_is_stored_as_null(string? email) =>
        Assert.Null(Create(email: email).Email);

    [Fact]
    public void Email_is_trimmed() =>
        Assert.Equal("ana@example.com", Create(email: "  ana@example.com ").Email);

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@")]
    [InlineData("@example.com")]
    [InlineData("ana@example")]
    [InlineData("ana silva@example.com")]
    [InlineData("ana..silva@example.com")]
    [InlineData("ana@@example.com")]
    public void Rejects_invalid_email(string email) =>
        Assert.Contains("Email", Errors(() => Create(email: email)).Keys);

    [Theory]
    [InlineData("CUSTOMER", PartnerType.Customer)]
    [InlineData("SUPPLIER", PartnerType.Supplier)]
    [InlineData("BOTH", PartnerType.Both)]
    public void Accepts_known_types(string code, PartnerType expected) =>
        Assert.Equal(expected, Create(type: code).Type);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("VENDOR")]
    [InlineData("customer")]
    [InlineData("0")]
    public void Rejects_unknown_types(string? code) =>
        Assert.Contains("Type", Errors(() => Create(type: code)).Keys);

    [Theory]
    [InlineData("0")]
    [InlineData("1000000")]
    [InlineData("10.99")]
    [InlineData("10.990")]   // trailing zero is not extra precision
    public void Accepts_credit_limits_in_range_with_up_to_two_decimals(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(amount, Create(creditLimit: amount).CreditLimit);
    }

    [Theory]
    [InlineData("10.999")]
    [InlineData("0.001")]
    [InlineData("-0.01")]
    [InlineData("1000000.01")]
    public void Rejects_out_of_range_or_over_precise_credit_limits(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("CreditLimit", Errors(() => Create(creditLimit: amount)).Keys);
    }

    [Fact]
    public void Reports_every_invalid_field_at_once()
    {
        var errors = Errors(() => PartnerDetails.Create(" ", "ABC", "X", "bad", 10.999m));

        Assert.Equal(["CreditLimit", "Email", "Name", "TaxNumber", "Type"], errors.Keys.Order());
    }
}
