using Application.Reportes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IPedidoReporteService
    {
        Task<ReporteArchivo> GenerarListadoAsync(
            ReportePedidosFiltro filtro,
            FormatoReporte formato,
            string? usuario,
            CancellationToken cancellationToken = default);

        Task<ReporteArchivo> GenerarComprobanteAsync(
            int pedidoId,
            FormatoReporte formato,
            string? usuario,
            CancellationToken cancellationToken = default);
    }
}
