using FSEdu.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Price).HasColumnType("decimal(10,2)");
        b.Property(x => x.Interval).HasConversion<int>();
        b.Property(x => x.FeaturesJson).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("Subscriptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Term).HasConversion<int?>();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.PaymentProvider).HasConversion<int>();
        b.Property(x => x.ProviderReference).HasMaxLength(255);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.AmountPaid).HasColumnType("decimal(10,2)");

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => new { x.UserId, x.Type, x.Status });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasColumnType("decimal(10,2)");
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Method).HasConversion<int?>();
        b.Property(x => x.ReferenceNumber).HasMaxLength(100);
        b.Property(x => x.ReceiptImageUrl).HasMaxLength(500);
        b.Property(x => x.SenderName).HasMaxLength(200);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.ReviewNote).HasMaxLength(500);
        b.Property(x => x.InvoiceUrl).HasMaxLength(500);

        b.HasOne<Subscription>().WithMany().HasForeignKey(x => x.SubscriptionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
        b.HasIndex(x => x.UserId);
    }
}

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.ToTable("Coupons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.DiscountPct).HasColumnType("decimal(5,2)");
        b.Property(x => x.DiscountFixed).HasColumnType("decimal(10,2)");
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SalesCampaignConfiguration : IEntityTypeConfiguration<SalesCampaign>
{
    public void Configure(EntityTypeBuilder<SalesCampaign> b)
    {
        b.ToTable("SalesCampaigns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.DiscountPct).HasColumnType("decimal(5,2)");
        b.Property(x => x.Scope).HasConversion<int>();

        b.HasIndex(x => new { x.Active, x.ValidFromUtc, x.ValidUntilUtc });
    }
}

public sealed class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> b)
    {
        b.ToTable("DiscountRules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.TitleAr).HasMaxLength(255).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(500);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.DiscountPct).HasColumnType("decimal(5,2)");

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.Active, x.Type });
    }
}
