using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Reportes
{
    public enum FormatoReporte
    {
        /// <summary>Vista previa: PDF en Windows, HTML en Linux.</summary>
        Vista,
        Pdf,
        Excel,
        Word
    }
    public sealed record ReporteArchivo(byte[] Contenido, string ContentType, string NombreArchivo);

    /// <summary>Filtros de GET /api/v1/Reportes/pedidos (query string).</summary>
    public sealed class ReportePedidosFiltro
    {
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public string? Estado { get; set; }
        public int? ClienteId { get; set; }
    }

    /*
     * Filas de los DataSets del .rdlc.
     * Los nombres de las propiedades deben coincidir EXACTAMENTE
     * con los <Field> del reporte.
     */
    public sealed class PedidoReporteFila
    {
        public int PedidoId { get; init; }
        public DateTime Fecha { get; init; }
        public string Cliente { get; init; } = string.Empty;
        public string? Cedula { get; init; }
        public string FormaPago { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public int CantidadProductos { get; init; }
        public decimal SubTotal { get; init; }
        public decimal Descuento { get; init; }
        public decimal IVA { get; init; }
        public decimal Total { get; init; }
    }

    public sealed class ComprobanteEncabezadoFila
    {
        public int PedidoId { get; init; }
        public DateTime Fecha { get; init; }
        public string ClienteNombre { get; init; } = string.Empty;
        public string? ClienteCedula { get; init; }
        public string? ClienteTelefono { get; init; }
        public string? ClienteDireccion { get; init; }
        public string FormaPago { get; init; } = string.Empty;
        public string Estado { get; init; } = string.Empty;
        public decimal SubTotal { get; init; }
        public decimal Descuento { get; init; }
        public decimal IVA { get; init; }
        public decimal Total { get; init; }
    }

    public sealed class ComprobanteDetalleFila
    {
        public string? ProductoCodigo { get; init; }
        public string ProductoNombre { get; init; } = string.Empty;
        public int Cantidad { get; init; }
        public decimal PrecioUnitario { get; init; }
        public decimal Descuento { get; init; }
        public decimal SubtotalLinea { get; init; }
        public decimal IVA { get; init; }
        public decimal TotalLinea { get; init; }
    }

}
