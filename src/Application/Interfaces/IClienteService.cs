using Application.Clientes.Commands;
using Application.Clientes.Queries;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IClienteService
    {
        Task<List<ClienteDto>> ObtenerTodosAsync();
        Task<ClienteDto?> ObtenerPorIdAsync(int id);
        Task<int> CrearClienteAsync(CrearClienteCommand command);
        Task<bool> ActualizarClienteAsync(int id, ActualizarClienteCommand command);
        Task<bool> EliminarClienteAsync(int id);
    }
}
