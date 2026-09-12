using Application.DetallePedido.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Pedidos.Commands
{
    public class ActualizarPedidoCommand
    {
        public int? ClienteId { get; set; }
        public string? FormaPago { get; set; }
        
        public List<DetallePedidoCommand>? Detalles { get; set; }
    }
}
