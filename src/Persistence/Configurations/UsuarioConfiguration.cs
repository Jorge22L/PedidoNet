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
        }
    }
}
