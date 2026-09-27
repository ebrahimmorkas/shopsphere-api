using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopSphere.Domain.Orders;
using ShopSphere.Domain.Products;

namespace ShopSphere.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.ComplexProperty(o => o.Total, total =>
        {
            total.Property(m => m.Amount).HasColumnName("total_amount").HasPrecision(18, 2);
            total.Property(m => m.Currency).HasColumnName("total_currency").HasMaxLength(3);
        });

        builder.ComplexProperty(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("shipping_street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("shipping_city").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("shipping_state").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("shipping_country").HasMaxLength(2);
        });

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => new { o.CustomerId, o.CreatedAtUtc });
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired();

        builder.ComplexProperty(i => i.UnitPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 2);
            price.Property(m => m.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
        });

        builder.Ignore(i => i.LineTotal);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
