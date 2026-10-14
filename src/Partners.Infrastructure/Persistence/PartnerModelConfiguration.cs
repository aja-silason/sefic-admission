using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Partners.Domain.Partners;

namespace Partners.Infrastructure.Persistence;

internal sealed class PartnerModelConfiguration : IEntityTypeConfiguration<PartnerModel>
{
    public const string TableName = "Partners";
    public const string TaxNumberUniqueIndex = "UX_Partners_TaxNumber";

    // Case-insensitive (accent-sensitive) collation for Name, independent of the database default.
    private const string NameCollation = "Latin1_General_100_CI_AS";

    private static readonly ValueConverter<DateTime, DateTime> UtcConverter =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<PartnerModel> builder)
    {
        builder.ToTable(TableName, table =>
        {
            // Defence in depth: the domain already enforces these rules.
            table.HasCheckConstraint("CK_Partners_TaxNumber_Digits",
                $"LEN([TaxNumber]) = {TaxNumber.Length} AND [TaxNumber] NOT LIKE '%[^0-9]%'");
            table.HasCheckConstraint("CK_Partners_Type",
                $"[Type] IN ({string.Join(", ", PartnerTypes.Codes.Select(c => $"'{c}'"))})");
            table.HasCheckConstraint("CK_Partners_CreditLimit_Range",
                $"[CreditLimit] >= {PartnerDetails.CreditLimitMin} AND [CreditLimit] <= {PartnerDetails.CreditLimitMax:0}");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasMaxLength(PartnerDetails.NameMaxLength)
            .UseCollation(NameCollation)
            .IsRequired();

        builder.Property(p => p.TaxNumber)
            .HasMaxLength(TaxNumber.Length)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(p => p.Email).HasMaxLength(PartnerDetails.EmailMaxLength);
        builder.Property(p => p.CreditLimit).HasPrecision(18, 2);
        builder.Property(p => p.CreatedAt).HasConversion(UtcConverter);
        builder.Property(p => p.UpdatedAt).HasConversion(UtcConverter);

        // SQL Server rowversion: changes on every write and is checked in the UPDATE's WHERE clause.
        builder.Property(p => p.Version).IsRowVersion();

        // Uniqueness of the NIF is guaranteed by the database (also for inactive rows and concurrent requests).
        builder.HasIndex(p => p.TaxNumber).IsUnique().HasDatabaseName(TaxNumberUniqueIndex);

        // Supports the listing's ORDER BY Name, Id (Id is the clustered key, so it is part of every index row).
        builder.HasIndex(p => p.Name).HasDatabaseName("IX_Partners_Name");
    }
}
