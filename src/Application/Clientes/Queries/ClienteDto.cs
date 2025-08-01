using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Clientes.Queries
{
    public class ClienteDto
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; }
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool EsConsumidorFinal {  get; set; }
    }
}
