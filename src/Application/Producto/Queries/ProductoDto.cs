using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Producto.Queries
{
    public class ProductoDto
    {
        public int ProductoId { get; set; }
        public string? Codigo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioVenta {  get; set; }
        public int Existencias { get; set; }
        public bool? TieneIVA { get; set; }
        public bool? TieneISC { get; set; }
    }
}
