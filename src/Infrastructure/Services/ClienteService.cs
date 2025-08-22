using Application.Clientes.Commands;
using Application.Clientes.Queries;
using Application.Interfaces;
using Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ClienteService : IClienteService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        // Constructor que recibe el contexto de la base de datos
        // Inyección de dependencias para facilitar el testing y la mantenibilidad
        public ClienteService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> ActualizarClienteAsync(int id, ActualizarClienteCommand command)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return false;

            //cliente.Nombre = command.Nombre ?? cliente.Nombre;
            //cliente.Cedula = command.Cedula ?? cliente.Cedula;
            //cliente.Telefono = command.Telefono ?? cliente.Telefono;
            //cliente.Direccion = command.Direccion ?? cliente.Direccion;
            //cliente.EsConsumidorFinal = command.EsConsumidorFinal;

            _mapper.Map(command, cliente);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> CrearClienteAsync(CrearClienteCommand command)
        {
            //var cliente = new Cliente
            //{
            //    Nombre = command.Nombre,
            //    Cedula = command.Cedula,
            //    Telefono = command.Telefono,
            //    Direccion = command.Direccion,
            //    EsConsumidorFinal = command.EsConsumidorFinal
            //};

            var cliente = _mapper.Map<Cliente>(command);

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return cliente.ClienteId;
        }

        public async Task<bool> EliminarClienteAsync(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return false;

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
        {
            //var cliente = _context.Clientes
            //    .Where(c => c.ClienteId == id)
            //    .Select(c => new ClienteDto()
            //    {
            //        ClienteId = c.ClienteId,
            //        Nombre = c.Nombre,
            //        Cedula = c.Cedula,
            //        Telefono = c.Telefono,
            //        Direccion = c.Direccion,
            //        EsConsumidorFinal = c.EsConsumidorFinal
            //    })
            //    .FirstOrDefaultAsync();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return null;

            return _mapper.Map<ClienteDto>(cliente);
        }

        public async Task<List<ClienteDto>> ObtenerTodosAsync()
        {
            //var clientes = _context.Clientes
            //    .Select(c => new ClienteDto
            //    {
            //        ClienteId = c.ClienteId,
            //        Nombre = c.Nombre,
            //        Cedula = c.Cedula,
            //        Telefono = c.Telefono,
            //        Direccion = c.Direccion,
            //        EsConsumidorFinal = c.EsConsumidorFinal
            //    })
            //    .ToListAsync();

            var productos = await _context.Clientes.ToListAsync();

            return _mapper.Map<List<ClienteDto>>(productos);
        }
    }
}
