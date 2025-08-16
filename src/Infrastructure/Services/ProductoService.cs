using Application.Interfaces;
using Application.Producto.Commands;
using Application.Producto.Queries;
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
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductoService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> ActualizarProductoAsync(int id, ActualizarProductoCommand command)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            //producto.Codigo = command.Codigo;
            //producto.Nombre = command.Nombre;
            //producto.PrecioVenta = command.PrecioVenta;
            //producto.Existencias = command.Existencias;
            //producto.TieneIVA = command.TieneIVA;
            //producto.TieneISC = command.TieneISC;

            _mapper.Map(command, producto);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CrearProductoAsync(CrearProductoCommand command)
        {
            //var producto = new Producto
            //{
            //    Codigo = command.Codigo,
            //    Nombre = command.Nombre,
            //    PrecioVenta = command.PrecioVenta,
            //    Existencias = command.Existencias,
            //    TieneIVA = command.TieneIVA,
            //    TieneISC = command.TieneISC,
            //};

            var producto = _mapper.Map<Producto>(command);

            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            return producto.ProductoId;
        }

        public async Task<bool> EliminarProductoAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return false;

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
        {
            //var producto = await _context.Productos
            //    .Where(p => p.ProductoId == id)
            //    .Select(p => new ProductoDto
            //    {
            //        ProductoId = p.ProductoId,
            //        Codigo = p.Codigo,
            //        Nombre = p.Nombre,
            //        PrecioVenta = p.PrecioVenta,
            //        Existencias = p.Existencias,
            //        TieneIVA = p.TieneIVA,
            //        TieneISC = p.TieneISC,
            //    })
            //    .FirstOrDefaultAsync();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return null;

            return _mapper.Map<ProductoDto>(producto);

        }

        public async Task<List<ProductoDto>> ObtenerTodosAsync()
        {
            //var productos = await _context.Productos
            //    .Select(p => new ProductoDto
            //    {
            //        ProductoId = p.ProductoId,
            //        Codigo = p.Codigo,
            //        Nombre = p.Nombre,
            //        PrecioVenta = p.PrecioVenta,
            //        Existencias = p.Existencias,
            //        TieneIVA = p.TieneIVA,
            //        TieneISC = p.TieneISC,
            //    })
            //    .ToListAsync();

            var productos = await _context.Productos.ToListAsync();

            return _mapper.Map<List<ProductoDto>>(productos);
        }
    }
}
