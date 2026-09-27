using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopSphere.Domain.Categories;
using ShopSphere.Domain.Products;

namespace ShopSphere.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.Property(p => p.Sku)
            .HasConversion(sku => sku.Value, value => Sku.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.ComplexProperty(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("price_amount").HasPrecision(18, 2);
            price.Property(m => m.Currency).HasColumnName("price_currency").HasMaxLength(3);
        });

        // PostgreSQL's xmin system column acts as a row version so concurrent stock updates
        // can't silently overwrite each other (prevents overselling).
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.Name);
    }
}
