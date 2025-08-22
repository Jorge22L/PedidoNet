using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DetallePedido.Commands
{
    public class DetallePedidoCommand
    {
        [Required]
        public int ProductoId { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }
        public decimal Descuento { get; set; }
        public bool TieneIVA { get; set; }
        public bool TieneISC { get; set; }

    }
}
