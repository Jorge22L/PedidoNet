using Application.Clientes.Commands;
using Application.Clientes.Queries;
using Application.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Domain.Repositories;
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
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitofWork _unitOfWork;
        private readonly IMapper _mapper;

        public ClienteService(IClienteRepository clienteRepository, IUnitofWork unitOfWork, IMapper mapper)
        {
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> ActualizarClienteAsync(int id, ActualizarClienteCommand command)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
            if(cliente == null)
            {
                return false;
            }

            _mapper.Map(command, cliente);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<int> CrearClienteAsync(CrearClienteCommand command)
        {
           var cliente = _mapper.Map<Cliente>(command);

            await _clienteRepository.AgregarAsync(cliente);

            await _unitOfWork.SaveChangesAsync();

            return cliente.ClienteId;
        }

        public async Task<bool> EliminarClienteAsync(int id)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
            if (cliente == null) return false;

            _clienteRepository.Eliminar(cliente);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ClienteDto?> ObtenerPorIdAsync(int id)
        {
            var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
            if (cliente == null) return null;

            return _mapper.Map<ClienteDto>(cliente);
        }

        public async Task<List<ClienteDto>> ObtenerTodosAsync()
        {
            var clientes = await _clienteRepository.ObtenerTodosAsync();

            return _mapper.Map<List<ClienteDto>>(clientes);
        }
    }
}
