using Application.Exceptions;
using Application.Interfaces;
using Application.Reportes;
using Microsoft.EntityFrameworkCore;
using Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Infrastructure.Reportes
{
    public sealed class ReportePedidoService : IPedidoReporteService
    {
        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-NI");

        private readonly ApplicationDbContext _context;
        private readonly RdlcRenderer _renderer;

        public ReportePedidoService(ApplicationDbContext context, RdlcRenderer renderer)
        {
            _context = context;
            _renderer = renderer;
        }


        public async Task<ReporteArchivo> GenerarComprobanteAsync(int pedidoId, FormatoReporte formato, string? usuario, CancellationToken cancellationToken = default)
        {
            var pedido = await _context.Pedidos.AsNoTracking()
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId, cancellationToken)
                ?? throw new NotFoundException($"El pedido {pedidoId} no existe.");

            var encabezado = new ComprobanteEncabezadoFila
            {
                PedidoId = pedido.PedidoId,
                Fecha = pedido.Fecha,
                ClienteNombre = pedido.Cliente.Nombre,
                ClienteCedula = pedido.Cliente.Cedula,
                ClienteTelefono = pedido.Cliente.Telefono,
                ClienteDireccion = pedido.Cliente.Direccion,
                FormaPago = pedido.FormaPago,
                Estado = pedido.Estado,
                SubTotal = pedido.SubTotal,
                Descuento = pedido.Descuento,
                IVA = pedido.IVA,
                Total = pedido.Total
            };

            // ImporteBruto, SubtotalLinea, IVA y TotalLinea son propiedades calculadas del dominio.
            var detalles = pedido.Detalles
                .OrderBy(d => d.DetalleId)
                .Select(d => new ComprobanteDetalleFila
                {
                    ProductoCodigo = d.Producto.Codigo,
                    ProductoNombre = d.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Descuento = d.Descuento,
                    SubtotalLinea = d.SubtotalLinea,
                    IVA = Math.Round(d.IVA, 2),
                    TotalLinea = Math.Round(d.TotalLinea, 2)
                })
                .ToList();

            return _renderer.Render(
                "ComprobantePedido",
                $"pedido_{pedido.PedidoId}",
                formato,
                new Dictionary<string, IEnumerable>
                {
                    ["Encabezado"] = new[] { encabezado },
                    ["Detalles"] = detalles
                },
                ParametrosComunes($"Pedido #{pedido.PedidoId}", usuario));
        }

        public async Task<ReporteArchivo> GenerarListadoAsync(ReportePedidosFiltro filtro, FormatoReporte formato, string? usuario, CancellationToken cancellationToken = default)
        {
            if (filtro.Desde is { } d && filtro.Hasta is { } h && d > h)
                throw new BusinessRuleException("La fecha inicial no puede ser mayor que la fecha final.");

            var consulta = _context.Pedidos.AsNoTracking();

            if (filtro.Desde is { } desde)
            {
                var inicio = desde.ToDateTime(TimeOnly.MinValue);
                consulta = consulta.Where(p => p.Fecha >= inicio);
            }

            if (filtro.Hasta is { } hasta)
            {
                var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
                consulta = consulta.Where(p => p.Fecha < fin);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
            {
                var estado = filtro.Estado.Trim();
                consulta = consulta.Where(p => p.Estado == estado);
            }

            string? nombreCliente = null;

            if (filtro.ClienteId is { } clienteId)
            {
                consulta = consulta.Where(p => p.ClienteId == clienteId);

                nombreCliente = await _context.Clientes.AsNoTracking()
                    .Where(c => c.ClienteId == clienteId)
                    .Select(c => c.Nombre)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var filas = await consulta
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.PedidoId)
                .Select(p => new PedidoReporteFila
                {
                    PedidoId = p.PedidoId,
                    Fecha = p.Fecha,
                    Cliente = p.Cliente.Nombre,
                    Cedula = p.Cliente.Cedula,
                    FormaPago = p.FormaPago,
                    Estado = p.Estado,
                    CantidadProductos = p.Detalles.Count,
                    SubTotal = p.SubTotal,
                    Descuento = p.Descuento,
                    IVA = p.IVA,
                    Total = p.Total
                })
                .ToListAsync(cancellationToken);

            var parametros = ParametrosComunes("Listado de pedidos", usuario);
            parametros["Filtros"] = DescribirFiltros(filtro, nombreCliente);

            return _renderer.Render(
                "ListadoPedidos",
                $"pedidos_{DateTime.Now:yyyyMMdd_HHmm}",
                formato,
                new Dictionary<string, IEnumerable> { ["Pedidos"] = filas },
                parametros);
        }

        private static Dictionary<string, string?> ParametrosComunes(string titulo, string? usuario) => new()
        {
            ["Titulo"] = titulo,
            ["GeneradoPor"] = string.IsNullOrWhiteSpace(usuario) ? "—" : usuario,
            ["FechaGeneracion"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm", Cultura)
        };

        private static string DescribirFiltros(ReportePedidosFiltro filtro, string? nombreCliente)
        {
            var partes = new List<string>
            {
                (filtro.Desde, filtro.Hasta) switch
                {
                    ({ } desde, { } hasta) => $"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}",
                    ({ } desde, null) => $"Desde {desde:dd/MM/yyyy}",
                    (null, { } hasta) => $"Hasta {hasta:dd/MM/yyyy}",
                    _ => "Todas las fechas"
                },
                $"Estado: {(string.IsNullOrWhiteSpace(filtro.Estado) ? "Todos" : filtro.Estado)}"
            };

            if (filtro.ClienteId.HasValue)
                partes.Add($"Cliente: {nombreCliente ?? $"#{filtro.ClienteId}"}");

            return string.Join(" · ", partes);
        }
    }
}
