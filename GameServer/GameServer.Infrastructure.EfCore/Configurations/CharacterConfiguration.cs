using Game.Core.Common;
using Game.Core.Common.Enums;
using Game.Core.Common.ValueObjetcs;
using Game.Core.Entities.Character;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameServer.Infrastructure.EfCore.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<CharacterEntity>
{
    public void Configure(EntityTypeBuilder<CharacterEntity> builder)
    {
        builder.HasBaseType<BaseEntity>();

        // CharacterConfiguration
        builder.ToTable("Characters");

        // Properties configuration
        builder.Property(c => c.AccountId)
            .IsRequired()
            .HasComment("ID da conta proprietária do personagem");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(20)
            .HasComment("Nome do personagem (3-20 caracteres, case-insensitive)");

        builder.Property(c => c.Vocation)
            .IsRequired()
            .HasDefaultValue(VocationEnum.None)
            .HasComment("Vocação do personagem ");

        builder.Property(c => c.Gender)
            .IsRequired()
            .HasDefaultValue(GenderEnum.None)
            .HasComment("Sexo do personagem");

        builder.Property(c => c.Direction)
            .IsRequired()
            .HasDefaultValue(DirectionEnum.South)
            .HasComment("Direção que o personagem está virado");
        
        builder.Property(c => c.Position)
            .IsRequired()
            .HasConversion(
                v => v.ToString(), // Converte MapPosition
                v => MapPosition.FromString(v) // Converte string de volta para MapPosition
            )
            .HasComment("Posição do personagem no mapa (X,Y,Z)");
        builder.Property(c => c.Speed)
            .IsRequired()
            .HasDefaultValue(40.0f)
            .HasComment("Velocidade de movimento do personagem");
        
        // Índices para performance
        // Índice único para nome (B-tree) - case-insensitive
        builder.HasIndex(i => i.Name)
            .HasDatabaseName("IX_Characters_Name_Unique")
            .IsUnique();

        builder.HasIndex(c => c.AccountId)
            .HasDatabaseName("IX_Characters_AccountId");

        builder.HasIndex(c => c.Vocation)
            .HasDatabaseName("IX_Characters_Class");

        builder.HasIndex(c => new { c.AccountId, c.IsActive })
            .HasDatabaseName("IX_Characters_AccountId_IsActive");
    }
}