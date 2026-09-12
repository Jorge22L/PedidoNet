using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ProductoImagen
    {
        public int ProductoImagenId { get; set; }
        public int ProductoId { get; set; }
        public string Ruta { get; set; } = string.Empty;
        public string? NombreOriginal { get; set; }
        public string? ContentType { get; set; }
        public bool EsPrincipal { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public Producto Producto { get; set; } = null!;
    }
}
