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

namespace Infrastructure.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly ApplicationDbContext _context;

        public PedidoService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> ActualizarPedidoAsync(int id, ActualizarPedidoCommand command)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return false;

            // Solo permitir actualizar pedidos pendientes
            if (pedido.Estado != "Pendiente")
            {
                throw new InvalidOperationException("Solo se pueden actualizar pedidos pendientes");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Restaurar stock de los productos del pedido original
                foreach (var detalle in pedido.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto != null)
                    {
                        producto.Existencias += detalle.Cantidad;
                    }
                }


                // Actualizar campos básicos
                if (command.ClienteId > 0) pedido.ClienteId = command.ClienteId;
                if (command.Fecha.HasValue) pedido.Fecha = command.Fecha.Value;
                if (command.Descuento.HasValue) pedido.Descuento = command.Descuento.Value;
                if (!string.IsNullOrEmpty(command.FormaPago)) pedido.FormaPago = command.FormaPago;
                if (!string.IsNullOrEmpty(command.Estado)) pedido.Estado = command.Estado;

                // Actualizar detalles si se proporcionan
                if (command.Detalles != null && command.Detalles.Any())
                {
                    // Eliminar detalles existentes
                    _context.DetallePedidos.RemoveRange(pedido.Detalles);
                    pedido.Detalles.Clear();

                    // Agregar nuevos detalles
                    var productosIds = command.Detalles.Select(d => d.ProductoId).ToList();
                    var productos = await _context.Productos
                        .Where(p => productosIds.Contains(p.ProductoId))
                        .ToListAsync();

                    foreach (var detalleCommand in command.Detalles)
                    {
                        var producto = productos.First(p => p.ProductoId == detalleCommand.ProductoId);

                        if (producto.Existencias < detalleCommand.Cantidad) throw new ArgumentException($"Stock insuficiente para el {producto.Nombre}. Stock disponible: {producto.Existencias}");

                        var detalle = new DetallePedido
                        {
                            ProductoId = detalleCommand.ProductoId,
                            Cantidad = detalleCommand.Cantidad,
                            PrecioUnitario = detalleCommand.PrecioUnitario,
                            Descuento = detalleCommand.Descuento,
                            TieneIVA = detalleCommand.TieneIVA,
                        };

                        pedido.Detalles.Add(detalle);
                        producto.Existencias -= detalleCommand.Cantidad;

                        // Recalcular Totales
                        CalcularTotalesPedido(pedido);

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return true;
                    }
                }

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return false;
        }

        // ✅ MÉTODO PRIVADO PARA CALCULAR TOTALES
        private void CalcularTotalesPedido(Pedido pedido)
        {
            decimal subtotal = 0;
            decimal totalIVA = 0;

            foreach (var detalle in pedido.Detalles)
            {
                var subtotalLinea = (detalle.Cantidad * detalle.PrecioUnitario) - detalle.Descuento;
                subtotal += subtotalLinea;

                // Calcular IVA
                if (detalle.TieneIVA)
                {
                    totalIVA += subtotalLinea * 0.15m;
                }

            }

            pedido.SubTotal = subtotal;
            pedido.IVA = totalIVA;

            pedido.Total = subtotal + totalIVA - pedido.Descuento;
        }

        public async Task<bool> CambiarEstadoPedidoAsync(int id, string nuevoEstado)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null) return false;

            var estadosValidos = new[] { "Pendiente", "Completado", "Cancelado" };
            if (!estadosValidos.Contains(nuevoEstado))
                throw new ArgumentException("Estado no válido. Estados permitidos: Pendiente, Completado, Cancelado");

            pedido.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CancelarPedidoAsync(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return false;

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Si el pedido está pendiente, restaurar stock
                if (pedido.Estado == "Pendiente")
                {
                    foreach (var detalle in pedido.Detalles)
                    {
                        var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                        if (producto != null)
                            producto.Existencias += detalle.Cantidad;
                    }
                }

                pedido.Estado = "Cancelado";
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CompletarPedidoAsync(int id)
        {
            return await CambiarEstadoPedidoAsync(id, "Completado");
        }

        public async Task<int> CrearPedidoAsync(CrearPedidoCommand command)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verificar que cliente existe
                var cliente = await _context.Clientes.AnyAsync(c => c.ClienteId == command.ClienteId);
                if (!cliente) throw new ArgumentException("El cliente especificado no existe");

                // Verificar que todos los productos existen y tiene stock suficiente
                var productosIds = command.Detalles.Select(d => d.ProductoId).ToList();
                var productos = await _context.Productos
                    .Where(p => productosIds.Contains(p.ProductoId))
                    .ToListAsync();

                if (productos.Count != productosIds.Count) throw new ArgumentException("Uno o más productos no existen");

                // Verificar stock
                foreach (var detalle in command.Detalles)
                {
                    var producto = productos.First(p => p.ProductoId == detalle.ProductoId);
                    if (producto.Existencias < detalle.Cantidad) throw new ArgumentException($"Stock insuficiente para el producto {producto.Nombre}. Stock disponible: {producto.Existencias}");
                }

                // Crear el pedido
                var pedido = new Pedido
                {
                    ClienteId = command.ClienteId,
                    Fecha = command.Fecha,
                    FormaPago = command.FormaPago,
                    Estado = "Pendiente",
                    Descuento = command.Descuento
                };

                // Crear los detalles
                foreach (var detalleCommand in command.Detalles)
                {
                    var producto = productos.First(p => p.ProductoId == detalleCommand.ProductoId);

                    var detalle = new DetallePedido
                    {
                        ProductoId = detalleCommand.ProductoId,
                        Cantidad = detalleCommand.Cantidad,
                        PrecioUnitario = detalleCommand.PrecioUnitario,
                        Descuento = detalleCommand.Descuento,
                        TieneIVA = detalleCommand.TieneIVA,

                    };

                    pedido.Detalles.Add(detalle);

                    // Actualizar Stock
                    producto.Existencias -= detalleCommand.Cantidad;

                    CalcularTotalesPedido(pedido);

                    _context.Pedidos.Add(pedido);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return pedido.PedidoId;

                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return 0;
        }

        public async Task<bool> EliminarPedidoAsync(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.PedidoId == id);

            if (pedido == null) return false;

            // Solo permitir eliminar pedidos pendientes
            if (pedido.Estado != "Pendiente")
                throw new InvalidOperationException("Solo se pueden eliminar pedidos pendientes");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Restaurar stock
                foreach (var detalle in pedido.Detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    if (producto != null)
                        producto.Existencias += detalle.Cantidad;
                }

                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PedidoDto>> ObtenerPorClienteAsync(int clienteId)
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.ClienteId == clienteId)
                .Select(p => new PedidoDto
                {
                    PedidoId = p.PedidoId,
                    ClienteId = p.ClienteId,
                    ClienteNombre = p.Cliente.Nombre,
                    Fecha = p.Fecha,
                    SubTotal = p.SubTotal,
                    IVA = p.IVA,
                    Descuento = p.Descuento,
                    Total = p.Total,
                    FormaPago = p.FormaPago,
                    Estado = p.Estado,
                    Detalles = p.Detalles.Select(d => new DetallePedidoDto
                    {
                        DetalleId = d.DetalleId,
                        ProductoId = d.ProductoId,
                        ProductoNombre = d.Producto.Nombre,
                        ProductoCodigo = d.Producto.Codigo,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Descuento = d.Descuento,
                        TieneIVA = d.TieneIVA,
                        TieneISC = d.Producto.TieneISC ?? false,
                        SubtotalLinea = d.SubtotalLinea,
                        IVA = d.IVA,
                    }).ToList()
                })
                .ToListAsync();

            return pedidos;
        }

        public async Task<PedidoDto>? ObtenerPorIdAsync(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Select(p => new PedidoDto
                {
                    PedidoId = p.PedidoId,
                    ClienteId = p.ClienteId,
                    ClienteNombre = p.Cliente.Nombre,
                    Fecha = p.Fecha,
                    SubTotal = p.SubTotal,
                    IVA = p.IVA,
                    Descuento = p.Descuento,
                    Total = p.Total,
                    FormaPago = p.FormaPago,
                    Estado = p.Estado,
                    Detalles = p.Detalles.Select(d => new DetallePedidoDto
                    {
                        DetalleId = d.DetalleId,
                        ProductoId = d.ProductoId,
                        ProductoNombre = d.Producto.Nombre,
                        ProductoCodigo = d.Producto.Codigo,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Descuento = d.Descuento,
                        TieneIVA = d.TieneIVA,
                        TieneISC = d.Producto.TieneISC ?? false,
                        SubtotalLinea = d.SubtotalLinea,
                        IVA = d.IVA
                    }).ToList()
                }).FirstOrDefaultAsync();

            return pedido;
        }

        public async Task<List<PedidoDto>> ObtenerTodosAsync()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Select(p => new PedidoDto
                {
                    PedidoId = p.PedidoId,
                    ClienteId = p.ClienteId,
                    ClienteNombre = p.Cliente.Nombre,
                    Fecha = p.Fecha,
                    SubTotal = p.SubTotal,
                    IVA = p.IVA,
                    Descuento = p.Descuento,
                    Total = p.Total,
                    FormaPago = p.FormaPago,
                    Estado = p.Estado,
                    Detalles = p.Detalles.Select(d => new DetallePedidoDto
                    {
                        DetalleId = d.DetalleId,
                        ProductoId = d.ProductoId,
                        ProductoNombre = d.Producto.Nombre,
                        ProductoCodigo = d.Producto.Codigo,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        Descuento = d.Descuento,
                        TieneIVA = d.TieneIVA,
                        TieneISC = d.Producto.TieneISC ?? false,
                        SubtotalLinea = d.SubtotalLinea,
                        IVA = d.IVA
                    }).ToList()
                }).ToListAsync();

            return pedidos;
        }

        
    }

    
    }
