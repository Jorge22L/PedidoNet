using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Producto.Commands;
using Application.Producto.Queries;
using Domain.Abstractions;
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
    public class ProductoService : IProductoService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IUnitofWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductoService(IProductoRepository productoRepository, IUnitofWork unitOfWork, IMapper mapper)
        {
            _productoRepository = productoRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> ActualizarProductoAsync(int id, ActualizarProductoCommand command)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return false;

            _mapper.Map(command, producto);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<int> CrearProductoAsync(CrearProductoCommand command)
        {
            var producto = _mapper.Map<Producto>(command);

            await _productoRepository.AgregarAsync(producto);

            await _unitOfWork.SaveChangesAsync();

            return producto.ProductoId;
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return false;

            _productoRepository.Eliminar(producto);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(id);
            if (producto == null) return null;

            return _mapper.Map<ProductoDto>(producto);

        }

        public async Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            var productos = await _productoRepository.ObtenerTodosAsync();

            return _mapper.Map<List<ProductoDto>>(productos);
        }
    }
}
