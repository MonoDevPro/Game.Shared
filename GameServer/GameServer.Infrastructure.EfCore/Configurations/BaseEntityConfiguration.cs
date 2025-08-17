using Game.Core.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServer.Infrastructure.EfCore.Configurations;

public class BaseEntityConfiguration : IEntityTypeConfiguration<BaseEntity>
{
    public void Configure(EntityTypeBuilder<BaseEntity> builder)
    {
        builder.UseTpcMappingStrategy();
        
        // Filtro global para soft delete
        builder.HasQueryFilter(a => a.IsActive);
        
        // Configurações comuns para entidades base
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        
        // Configurações adicionais
        builder.Property(c => c.IsActive)
            .HasDefaultValue(true)
            .HasComment("Flag para soft delete");
    }
}