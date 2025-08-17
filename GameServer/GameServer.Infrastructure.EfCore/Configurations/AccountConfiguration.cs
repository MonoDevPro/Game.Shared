using Game.Core.Common;
using Game.Core.Entities.Account;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServer.Infrastructure.EfCore.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.HasBaseType<BaseEntity>();

        // Table mapping
        builder.ToTable("Accounts");

        // Characters relationship configuration
        builder.HasMany(a => a.Characters)
            .WithOne(c => c.AccountEntity)
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices para performance
        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_Accounts_IsActive");
    }
}