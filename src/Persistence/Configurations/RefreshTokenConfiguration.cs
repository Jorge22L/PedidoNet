using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");
            builder.HasKey(x => x.RefreshTokenId);

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.CreadoEn)
                .IsRequired();

            builder.Property(x => x.ExpiraEn)
                .IsRequired();

            builder.Property(x => x.ReemplazadoPorTokenHash)
                .HasMaxLength(200);

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.HasOne(x => x.Usuario)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
