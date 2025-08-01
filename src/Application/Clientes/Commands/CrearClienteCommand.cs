using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Clientes.Commands
{
    public class CrearClienteCommand
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool EsConsumidorFinal { get; set; } = true;
    }
}
