using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Configurations
{
    public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.HasKey(p => p.ProductoId);

            builder.Property(p => p.ClientId)
               .IsRequired(false);

            builder.HasIndex(p => p.ClientId)
                .IsUnique()
                .HasFilter("[ClientId] IS NOT NULL");

            builder.Property(p => p.Codigo)
                .HasMaxLength(20);

            builder.Property(p => p.Nombre)
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
