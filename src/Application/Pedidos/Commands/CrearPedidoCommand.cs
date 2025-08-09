using Application.DetallePedido.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pedidos.Commands
{
    public class CrearPedidoCommand
    {
        [Required]
        public int ClienteId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Descuento { get; set; }
        [Required]
        public string FormaPago { get; set; } = "Contado";

        [Required]
        public List<DetallePedidoCommand> Detalles { get; set; } = new List<DetallePedidoCommand>();

    }
}
