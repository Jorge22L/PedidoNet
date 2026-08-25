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

namespace Infrastructure.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public PedidoService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
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
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    {"Mensaje", new[] { "Solo se pueden actualizar pedidos pendientes "} }
                });
                
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
                //if (command.ClienteId > 0) pedido.ClienteId = command.ClienteId;
                //if (command.Fecha.HasValue) pedido.Fecha = command.Fecha.Value;
                //if (command.Descuento.HasValue) pedido.Descuento = command.Descuento.Value;
                //if (!string.IsNullOrEmpty(command.FormaPago)) pedido.FormaPago = command.FormaPago;
                //if (!string.IsNullOrEmpty(command.Estado)) pedido.Estado = command.Estado;

                _mapper.Map(command, pedido);

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
                // 1. Validad existencia del cliente
                var cliente = await _context.Clientes.AnyAsync(c => c.ClienteId == command.ClienteId);
                if (!cliente) throw new ArgumentException("El cliente especificado no existe");

                // 2. Evitar productos repetidos
                var productosIds = command.Detalles.Select(d => d.ProductoId).ToList();
                if(productosIds.Count != productosIds.Distinct().Count())
                {
                    throw new ArgumentException("No se permite agregar el mismo producto más de una vez en el pedido");
                }

                // 3. Obtener productos desde DB
                var productos = await _context.Productos
                    .Where(p => productosIds.Contains(p.ProductoId))
                    .ToListAsync();

                if (productos.Count != productosIds.Count) throw new ArgumentException("Uno o más productos no existen");

                // Diccionario para evitar First() repetidos
                var productosPorId = productos.ToDictionary(p => p.ProductoId);

                // 4. Validar stock de TODOS los productos antes de modificar
                foreach (var detalleCommand in command.Detalles)
                {
                    var producto = productosPorId[detalleCommand.ProductoId];
                    if (producto.Existencias < detalleCommand.Cantidad)
                    {
                        throw new ArgumentException(
                            $"Stock insuficiente para el producto {producto.Nombre}." +
                            $"Stock disponible: {producto.Existencias}");
                    }
                }

                // 5. Crear encabezado
                var pedido = _mapper.Map<Pedido>(command);
                pedido.Estado = "Pendiente";
                pedido.Detalles.Clear();

                // 6. Crear los detalles
                foreach (var detalleCommand in command.Detalles)
                {
                    var producto = productosPorId[detalleCommand.ProductoId];

                    var detalle = new DetallePedido
                    {
                        ProductoId = detalleCommand.ProductoId,
                        Cantidad = detalleCommand.Cantidad,

                        // Nunca confiar en el precio enviado por el cliente
                        PrecioUnitario = producto.PrecioVenta,
                        Descuento = detalleCommand.Descuento,

                        // El impuesto también viene del producto
                        TieneIVA = producto.TieneIVA ?? false
                    };

                    pedido.Detalles.Add(detalle);

                    // Actualizar Stock
                    producto.Existencias -= detalleCommand.Cantidad;
                }
                    // 7. Calcular una sola vez
                    CalcularTotalesPedido(pedido);

                    // 8. Persistir una sola vez
                    _context.Pedidos.Add(pedido);

                    await _context.SaveChangesAsync();

                    // 9. Confirmar cuando TODO fue exitoso
                    await transaction.CommitAsync();

                    return pedido.PedidoId;

                
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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
            //var pedidos = await _context.Pedidos
            //    .Include(p => p.Cliente)
            //    .Include(p => p.Detalles)
            //        .ThenInclude(d => d.Producto)
            //    .Where(p => p.ClienteId == clienteId)
            //    .Select(p => new PedidoDto
            //    {
            //        PedidoId = p.PedidoId,
            //        ClienteId = p.ClienteId,
            //        ClienteNombre = p.Cliente.Nombre,
            //        Fecha = p.Fecha,
            //        SubTotal = p.SubTotal,
            //        IVA = p.IVA,
            //        Descuento = p.Descuento,
            //        Total = p.Total,
            //        FormaPago = p.FormaPago,
            //        Estado = p.Estado,
            //        Detalles = p.Detalles.Select(d => new DetallePedidoDto
            //        {
            //            DetalleId = d.DetalleId,
            //            ProductoId = d.ProductoId,
            //            ProductoNombre = d.Producto.Nombre,
            //            ProductoCodigo = d.Producto.Codigo,
            //            Cantidad = d.Cantidad,
            //            PrecioUnitario = d.PrecioUnitario,
            //            Descuento = d.Descuento,
            //            TieneIVA = d.TieneIVA,
            //            TieneISC = d.Producto.TieneISC ?? false,
            //            SubtotalLinea = d.SubtotalLinea,
            //            IVA = d.IVA,
            //        }).ToList()
            //    })
            //    .ToListAsync();

            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
                .Where(p => p.ClienteId == clienteId)
                .ProjectToType<PedidoDto>()
                .ToListAsync();

            return pedidos;
        }

        public async Task<PedidoDto>? ObtenerPorIdAsync(int id)
        {
            //var pedido = await _context.Pedidos
            //    .Include(p => p.Cliente)
            //    .Include(p => p.Detalles)
            //    .ThenInclude(d => d.Producto)
            //    .Select(p => new PedidoDto
            //    {
            //        PedidoId = p.PedidoId,
            //        ClienteId = p.ClienteId,
            //        ClienteNombre = p.Cliente.Nombre,
            //        Fecha = p.Fecha,
            //        SubTotal = p.SubTotal,
            //        IVA = p.IVA,
            //        Descuento = p.Descuento,
            //        Total = p.Total,
            //        FormaPago = p.FormaPago,
            //        Estado = p.Estado,
            //        Detalles = p.Detalles.Select(d => new DetallePedidoDto
            //        {
            //            DetalleId = d.DetalleId,
            //            ProductoId = d.ProductoId,
            //            ProductoNombre = d.Producto.Nombre,
            //            ProductoCodigo = d.Producto.Codigo,
            //            Cantidad = d.Cantidad,
            //            PrecioUnitario = d.PrecioUnitario,
            //            Descuento = d.Descuento,
            //            TieneIVA = d.TieneIVA,
            //            TieneISC = d.Producto.TieneISC ?? false,
            //            SubtotalLinea = d.SubtotalLinea,
            //            IVA = d.IVA
            //        }).ToList()
            //    }).FirstOrDefaultAsync();

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .Where(p => p.PedidoId == id)
                .ProjectToType<PedidoDto>() // Usar ProjectToType de Mapster
                .FirstOrDefaultAsync();

            return pedido;
        }

        public async Task<List<PedidoDto>> ObtenerTodosAsync()
        {
            //var pedidos = await _context.Pedidos
            //    .Include(p => p.Cliente)
            //    .Include(p => p.Detalles)
            //    .ThenInclude(d => d.Producto)
            //    .Select(p => new PedidoDto
            //    {
            //        PedidoId = p.PedidoId,
            //        ClienteId = p.ClienteId,
            //        ClienteNombre = p.Cliente.Nombre,
            //        Fecha = p.Fecha,
            //        SubTotal = p.SubTotal,
            //        IVA = p.IVA,
            //        Descuento = p.Descuento,
            //        Total = p.Total,
            //        FormaPago = p.FormaPago,
            //        Estado = p.Estado,
            //        Detalles = p.Detalles.Select(d => new DetallePedidoDto
            //        {
            //            DetalleId = d.DetalleId,
            //            ProductoId = d.ProductoId,
            //            ProductoNombre = d.Producto.Nombre,
            //            ProductoCodigo = d.Producto.Codigo,
            //            Cantidad = d.Cantidad,
            //            PrecioUnitario = d.PrecioUnitario,
            //            Descuento = d.Descuento,
            //            TieneIVA = d.TieneIVA,
            //            TieneISC = d.Producto.TieneISC ?? false,
            //            SubtotalLinea = d.SubtotalLinea,
            //            IVA = d.IVA
            //        }).ToList()
            //    }).ToListAsync();

            var pedidos = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .ProjectToType<PedidoDto>() // Usar ProjectToType de Mapster
                .ToListAsync();

            return pedidos;
        }

        
    }

    
    }
