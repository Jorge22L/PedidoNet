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
using Application.Interfaces.Repositories;
using Domain.Abstractions;

namespace Infrastructure.Services
{
    public class PedidoService : IPedidoService
    {
        private const decimal TasaIva = 0.15m;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IProductoRepository _productoRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IUnitofWork _unitOfWork;
        private readonly IMapper _mapper;

        public PedidoService(IPedidoRepository pedidoRepository, IProductoRepository productoRepository,
            IClienteRepository clienteRepository, IUnitofWork unitOfWork, IMapper mapper)
        {
            _pedidoRepository = pedidoRepository;
            _productoRepository = productoRepository;
            _clienteRepository = clienteRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<int> CrearPedidoAsync(CrearPedidoCommand command)
        {
            await using var transaction =
                await _unitOfWork
                    .BeginTransactionAsync();

            try
            {
                var clienteExiste =
                    await _clienteRepository
                        .ExisteAsync(command.ClienteId);

                if (!clienteExiste)
                {
                    throw new ArgumentException(
                        "El cliente especificado no existe.");
                }

                var productosIds = command.Detalles
                    .Select(d => d.ProductoId)
                    .ToList();

                if (productosIds.Count !=
                    productosIds.Distinct().Count())
                {
                    throw new ArgumentException(
                        "No se permite repetir un producto dentro del pedido.");
                }

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(
                            productosIds);

                if (productos.Count !=
                    productosIds.Count)
                {
                    throw new ArgumentException(
                        "Uno o más productos no existen.");
                }

                var productosPorId =
                    productos.ToDictionary(
                        p => p.ProductoId);

                // Primero validar TODO el stock
                foreach (var detalleCommand
                         in command.Detalles)
                {
                    var producto =
                        productosPorId[
                            detalleCommand.ProductoId];

                    if (producto.Existencias <
                        detalleCommand.Cantidad)
                    {
                        throw new ArgumentException(
                            $"Stock insuficiente para " +
                            $"el producto {producto.Nombre}. " +
                            $"Stock disponible: " +
                            $"{producto.Existencias}");
                    }
                }

                var pedido =
                    _mapper.Map<Pedido>(command);

                pedido.Estado = "Pendiente";

                /*
                 * Importante:
                 * si Mapster está mapeando automáticamente
                 * command.Detalles, limpiamos la colección para
                 * construirla nosotros con precios de BD.
                 */
                pedido.Detalles.Clear();

                foreach (var detalleCommand
                         in command.Detalles)
                {
                    var producto =
                        productosPorId[
                            detalleCommand.ProductoId];

                    var detalle =
                        new DetallePedido
                        {
                            ProductoId =
                                producto.ProductoId,

                            Cantidad =
                                detalleCommand.Cantidad,

                            // Precio desde BD
                            PrecioUnitario =
                                producto.PrecioVenta,

                            Descuento =
                                detalleCommand.Descuento,

                            // IVA desde BD
                            TieneIVA =
                                producto.TieneIVA ?? false
                        };

                    pedido.Detalles.Add(detalle);

                    producto.Existencias -=
                        detalleCommand.Cantidad;
                }

                CalcularTotalesPedido(pedido);

                await _pedidoRepository
                    .AgregarAsync(pedido);

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                return pedido.PedidoId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ActualizarPedidoAsync(
            int id,
            ActualizarPedidoCommand command)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(
                        id,
                        incluirDetalles: true);

            if (pedido == null)
                return false;

            if (pedido.Estado != "Pendiente")
            {
                throw new ValidationException(
                    new Dictionary<string, string[]>
                    {
                    {
                        "Estado",
                        new[]
                        {
                            "Solo se pueden actualizar pedidos pendientes."
                        }
                    }
                    });
            }

            await using var transaction =
                await _unitOfWork
                    .BeginTransactionAsync();

            try
            {
                /*
                 * Actualizar cliente solamente cuando
                 * viene informado.
                 */
                if (command.ClienteId > 0)
                {
                    var clienteExiste =
                        await _clienteRepository
                            .ExisteAsync(
                                command.ClienteId);

                    if (!clienteExiste)
                    {
                        throw new ArgumentException(
                            "El cliente especificado no existe.");
                    }

                    pedido.ClienteId =
                        command.ClienteId;
                }

                if (command.Fecha.HasValue)
                {
                    pedido.Fecha =
                        command.Fecha.Value;
                }

                if (command.Descuento.HasValue)
                {
                    pedido.Descuento =
                        command.Descuento.Value;
                }

                if (!string.IsNullOrWhiteSpace(
                        command.FormaPago))
                {
                    pedido.FormaPago =
                        command.FormaPago;
                }

                /*
                 * Sólo restauramos y reconstruimos
                 * inventario si realmente vienen detalles.
                 */
                if (command.Detalles is
                    { Count: > 0 })
                {
                    /*
                     * 1. Obtener productos anteriores.
                     */
                    var productosAnterioresIds =
                        pedido.Detalles
                            .Select(
                                d => d.ProductoId)
                            .Distinct()
                            .ToList();

                    var productosAnteriores =
                        await _productoRepository
                            .ObtenerPorIdsAsync(
                                productosAnterioresIds);

                    var productosAnterioresPorId =
                        productosAnteriores
                            .ToDictionary(
                                p => p.ProductoId);

                    /*
                     * 2. Restaurar stock anterior.
                     */
                    foreach (var detalle
                             in pedido.Detalles)
                    {
                        if (
                            productosAnterioresPorId
                                .TryGetValue(
                                    detalle.ProductoId,
                                    out var producto))
                        {
                            producto.Existencias +=
                                detalle.Cantidad;
                        }
                    }

                    /*
                     * 3. Validar productos duplicados.
                     */
                    var nuevosProductosIds =
                        command.Detalles
                            .Select(
                                d => d.ProductoId)
                            .ToList();

                    if (
                        nuevosProductosIds.Count !=
                        nuevosProductosIds
                            .Distinct()
                            .Count())
                    {
                        throw new ArgumentException(
                            "No se permite repetir un producto dentro del pedido.");
                    }

                    /*
                     * 4. Cargar nuevos productos.
                     */
                    var nuevosProductos =
                        await _productoRepository
                            .ObtenerPorIdsAsync(
                                nuevosProductosIds);

                    if (
                        nuevosProductos.Count !=
                        nuevosProductosIds.Count)
                    {
                        throw new ArgumentException(
                            "Uno o más productos no existen.");
                    }

                    var nuevosProductosPorId =
                        nuevosProductos
                            .ToDictionary(
                                p => p.ProductoId);

                    /*
                     * 5. Validar TODO el stock antes
                     *    de modificar los detalles.
                     */
                    foreach (var detalleCommand
                             in command.Detalles)
                    {
                        var producto =
                            nuevosProductosPorId[
                                detalleCommand.ProductoId];

                        if (
                            producto.Existencias <
                            detalleCommand.Cantidad)
                        {
                            throw new ArgumentException(
                                $"Stock insuficiente para " +
                                $"{producto.Nombre}. " +
                                $"Stock disponible: " +
                                $"{producto.Existencias}");
                        }
                    }

                    /*
                     * Guardamos una copia porque RemoveRange
                     * recibe los detalles existentes.
                     */
                    var detallesAnteriores =
                        pedido.Detalles.ToList();

                    _pedidoRepository
                        .EliminarDetalles(
                            detallesAnteriores);

                    pedido.Detalles.Clear();

                    /*
                     * 6. Crear TODOS los nuevos detalles.
                     */
                    foreach (var detalleCommand
                             in command.Detalles)
                    {
                        var producto =
                            nuevosProductosPorId[
                                detalleCommand.ProductoId];

                        var detalle =
                            new DetallePedido
                            {
                                ProductoId =
                                    producto.ProductoId,

                                Cantidad =
                                    detalleCommand.Cantidad,

                                PrecioUnitario =
                                    producto.PrecioVenta,

                                Descuento =
                                    detalleCommand.Descuento,

                                TieneIVA =
                                    producto.TieneIVA ??
                                    false
                            };

                        pedido.Detalles.Add(
                            detalle);

                        producto.Existencias -=
                            detalleCommand.Cantidad;
                    }
                }

                CalcularTotalesPedido(pedido);

                await _unitOfWork
                    .SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool>
            CambiarEstadoPedidoAsync(
                int id,
                string nuevoEstado)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(id);

            if (pedido == null)
                return false;

            var estadosValidos =
                new[]
                {
                "Pendiente",
                "Completado",
                "Cancelado"
                };

            if (!estadosValidos.Contains(
                    nuevoEstado))
            {
                throw new ArgumentException(
                    "Estado no válido. " +
                    "Estados permitidos: " +
                    "Pendiente, Completado, Cancelado.");
            }

            /*
             * Manteniendo la lógica del punto 5:
             * solamente Pendiente puede cambiar.
             */
            if (pedido.Estado != "Pendiente")
            {
                throw new InvalidOperationException(
                    $"No se puede cambiar un pedido " +
                    $"que está en estado " +
                    $"'{pedido.Estado}'.");
            }

            if (nuevoEstado == "Pendiente")
            {
                throw new InvalidOperationException(
                    "El pedido ya se encuentra pendiente.");
            }

            pedido.Estado = nuevoEstado;

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool>
            CompletarPedidoAsync(int id)
        {
            return await CambiarEstadoPedidoAsync(
                id,
                "Completado");
        }

        public async Task<bool>
            CancelarPedidoAsync(int id)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(
                        id,
                        incluirDetalles: true);

            if (pedido == null)
                return false;

            if (pedido.Estado != "Pendiente")
            {
                throw new InvalidOperationException(
                    "Solo se pueden cancelar " +
                    "pedidos pendientes.");
            }

            await using var transaction =
                await _unitOfWork
                    .BeginTransactionAsync();

            try
            {
                var productosIds =
                    pedido.Detalles
                        .Select(
                            d => d.ProductoId)
                        .Distinct()
                        .ToList();

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(
                            productosIds);

                var productosPorId =
                    productos.ToDictionary(
                        p => p.ProductoId);

                foreach (var detalle
                         in pedido.Detalles)
                {
                    if (
                        productosPorId.TryGetValue(
                            detalle.ProductoId,
                            out var producto))
                    {
                        producto.Existencias +=
                            detalle.Cantidad;
                    }
                }

                pedido.Estado = "Cancelado";

                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool>
            EliminarPedidoAsync(int id)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(
                        id,
                        incluirDetalles: true);

            if (pedido == null)
                return false;

            if (pedido.Estado != "Pendiente")
            {
                throw new InvalidOperationException(
                    "Solo se pueden eliminar " +
                    "pedidos pendientes.");
            }

            await using var transaction =
                await _unitOfWork
                    .BeginTransactionAsync();

            try
            {
                var productosIds =
                    pedido.Detalles
                        .Select(
                            d => d.ProductoId)
                        .Distinct()
                        .ToList();

                var productos =
                    await _productoRepository
                        .ObtenerPorIdsAsync(
                            productosIds);

                var productosPorId =
                    productos.ToDictionary(
                        p => p.ProductoId);

                foreach (var detalle
                         in pedido.Detalles)
                {
                    if (
                        productosPorId.TryGetValue(
                            detalle.ProductoId,
                            out var producto))
                    {
                        producto.Existencias +=
                            detalle.Cantidad;
                    }
                }

                _pedidoRepository
                    .Eliminar(pedido);

                await _unitOfWork
                    .SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PedidoDto?>
            ObtenerPorIdAsync(int id)
        {
            var pedido =
                await _pedidoRepository
                    .ObtenerPorIdAsync(
                        id,
                        incluirRelaciones: true);

            if (pedido == null)
                return null;

            return _mapper.Map<PedidoDto>(
                pedido);
        }

        public async Task<List<PedidoDto>>
            ObtenerTodosAsync()
        {
            var pedidos =
                await _pedidoRepository
                    .ObtenerTodosConRelacionesAsync();

            return _mapper.Map<List<PedidoDto>>(
                pedidos);
        }

        public async Task<List<PedidoDto>>
            ObtenerPorClienteAsync(
                int clienteId)
        {
            var pedidos =
                await _pedidoRepository
                    .ObtenerPorClienteConRelacionesAsync(
                        clienteId);

            return _mapper.Map<List<PedidoDto>>(
                pedidos);
        }

        private static void CalcularTotalesPedido(
            Pedido pedido)
        {
            decimal subtotal = 0;
            decimal totalIva = 0;

            foreach (var detalle
                     in pedido.Detalles)
            {
                var importeBruto =
                    detalle.Cantidad *
                    detalle.PrecioUnitario;

                if (
                    detalle.Descuento >
                    importeBruto)
                {
                    throw new ArgumentException(
                        "El descuento de una línea " +
                        "no puede superar su importe.");
                }

                var subtotalLinea =
                    importeBruto -
                    detalle.Descuento;

                subtotal += subtotalLinea;

                if (detalle.TieneIVA)
                {
                    totalIva +=
                        subtotalLinea * TasaIva;
                }
            }

            var totalAntesDescuento =
                subtotal + totalIva;

            if (
                pedido.Descuento >
                totalAntesDescuento)
            {
                throw new ArgumentException(
                    "El descuento general no puede " +
                    "superar el total del pedido.");
            }

            pedido.SubTotal = subtotal;
            pedido.IVA = totalIva;
            pedido.Total =
                totalAntesDescuento -
                pedido.Descuento;
        }

    }
}