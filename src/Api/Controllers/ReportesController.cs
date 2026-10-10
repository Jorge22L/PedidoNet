using Api.RateLimiting;
using Application.Interfaces;
using Application.Reportes;
using Asp.Versioning;
using Infrastructure.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize(Policy = "Pedidos.Read")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportesController : ControllerBase
    {
        private readonly IPedidoReporteService _reportes;

        public ReportesController(IPedidoReporteService reportes)
        {
            _reportes = reportes;
        }

        /// <summary>
        /// Listado de pedidos.
        /// GET /api/v1/Reportes/pedidos?desde=2026-10-01&amp;hasta=2026-10-31&amp;estado=Pendiente&amp;clienteId=5&amp;formato=vista
        /// </summary>
        [HttpGet("pedidos")]
        public async Task<IActionResult> ListadoPedidos(
            [FromQuery] ReportePedidosFiltro filtro,
            [FromQuery] FormatoReporte formato = FormatoReporte.Vista,
            CancellationToken cancellationToken = default)
        {
            var archivo = await _reportes.GenerarListadoAsync(
                filtro, formato, User.Identity?.Name, cancellationToken);

            return Archivo(archivo, formato);
        }

        /// <summary>Comprobante de un pedido. GET /api/v1/Reportes/pedidos/15?formato=pdf</summary>
        [HttpGet("pedidos/{id:int}")]
        public async Task<IActionResult> ComprobantePedido(
            int id,
            [FromQuery] FormatoReporte formato = FormatoReporte.Vista,
            CancellationToken cancellationToken = default)
        {
            var archivo = await _reportes.GenerarComprobanteAsync(
                id, formato, User.Identity?.Name, cancellationToken);

            return Archivo(archivo, formato);
        }

        // Vista previa: inline (sin nombre). Exportación: attachment con nombre.
        private FileContentResult Archivo(ReporteArchivo archivo, FormatoReporte formato) =>
            formato == FormatoReporte.Vista
                ? File(archivo.Contenido, archivo.ContentType)
                : File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}

