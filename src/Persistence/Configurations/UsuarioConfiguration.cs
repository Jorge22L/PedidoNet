using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");
            builder.HasKey(x => x.UsuarioId);
            builder.Property(x => x.NombreUsuario)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.NombreUsuario)
                .IsUnique();

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Rol)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Activo)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(256);

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");

            builder.Property(x => x.EntraObjectId)
                .HasMaxLength(64);

            builder.HasIndex(x => x.EntraObjectId)
                .IsUnique()
                .HasFilter("[EntraObjectId] IS NOT NULL");
        }
    }
}
