using Application.Interfaces;
using Application.Pedidos.Commands;
using Application.Pedidos.Queries;
using Application.DetallePedido.Queries;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;
using Application.Exceptions;
using MapsterMapper;
using Mapster;
using Domain.Constantes;
using Domain.Repositories;
using Domain.Abstractions;

namespace Infrastructure.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPedidoWriteRepository _pedidoWriteRepository;
        private readonly IMapper _mapper;
        

        public PedidoService(IPedidoRepository pedidoRepository, IPedidoWriteRepository pedidoWriteRepository, IMapper mapper)
        {
            _pedidoRepository = pedidoRepository;
            _pedidoWriteRepository = pedidoWriteRepository;
            _mapper = mapper;
        }

        public async Task<int> CrearPedidoAsync(CrearPedidoCommand command)
        {
            return await _pedidoWriteRepository.GuardarAsync(command);
        }

        public async Task<bool> ActualizarPedidoAsync(int id,ActualizarPedidoCommand command)
        {
            return await _pedidoWriteRepository.ActualizarAsync(id, command);
        }

        public async Task<bool>CompletarPedidoAsync(int id)
        {
            return await _pedidoWriteRepository.CompletarAsync(id);
        }

        public async Task<bool>CancelarPedidoAsync(int id)
        {
            return await _pedidoWriteRepository.CancelarAsync(id);
        }

        public async Task<bool>EliminarPedidoAsync(int id)
        {
            return await _pedidoWriteRepository.EliminarAsync(id);
        }

        public async Task<PedidoDto?>ObtenerPorIdAsync(int id)
        {
            var pedido = await _pedidoRepository.ObtenerPorIdAsync(id,incluirRelaciones: true);

            if (pedido == null)
                return null;

            return _mapper.Map<PedidoDto>(pedido);
        }

        public async Task<List<PedidoDto>>ObtenerTodosAsync()
        {
            var pedidos =await _pedidoRepository.ObtenerTodosConRelacionesAsync();

            return _mapper.Map<List<PedidoDto>>(pedidos);
        }

        public async Task<List<PedidoDto>>ObtenerPorClienteAsync(int clienteId)
        {
            var pedidos =await _pedidoRepository.ObtenerPorClienteConRelacionesAsync(clienteId);

            return _mapper.Map<List<PedidoDto>>(pedidos);
        }
        
    }
}