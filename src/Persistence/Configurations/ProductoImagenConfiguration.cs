using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence.Configurations
{
    public class ProductoImagenConfiguration : IEntityTypeConfiguration<ProductoImagen>
    {
        public void Configure(EntityTypeBuilder<ProductoImagen> builder)
        {
            builder.HasKey(x => x.ProductoImagenId);

            builder.Property(x => x.Ruta)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.NombreOriginal)
                .HasMaxLength(255);

            builder.Property(x => x.ContentType)
                .HasMaxLength(100);

            builder.HasOne(x => x.Producto)
                .WithMany(x => x.Imagenes)
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
