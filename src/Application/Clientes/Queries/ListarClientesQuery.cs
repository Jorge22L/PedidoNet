using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Clientes.Queries
{
    public class ListarClientesQuery
    {
        // Este es un ejemplo de una consulta que podría usarse para listar clientes.
        // En un escenario real, podrías tener propiedades para filtrar, ordenar o paginar los resultados.
        public Func<Cliente, bool>? Filtro { get; set; }
    }
}
