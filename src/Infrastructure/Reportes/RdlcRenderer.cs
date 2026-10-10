using Application.Exceptions;
using Application.Reportes;
using Microsoft.Reporting.NETCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Reportes
{
    /*
    * Renderiza los .rdlc embebidos con ReportViewer Core.
    *
    * - LocalReport no es thread-safe: se crea uno por solicitud.
    * - La primera ejecución tarda unos segundos (compila las expresiones).
    * - PDF necesita Windows (Uniscribe). En Linux la vista previa sale en HTML5.
    */
    public class RdlcRenderer
    {
        private static readonly System.Reflection.Assembly Ensamblado = typeof(RdlcRenderer).Assembly;

        public static bool SoportaPdf => OperatingSystem.IsWindows();

        public static bool ExisteDefinicion(string reporte) =>
            Ensamblado.GetManifestResourceInfo(NombreRecurso(reporte)) is not null;

        public ReporteArchivo Render(
            string reporte,
            string nombreArchivo,
            FormatoReporte formato,
            IReadOnlyDictionary<string, IEnumerable> dataSets,
            IReadOnlyDictionary<string, string?> parametros)
        {
            var (renderFormat, contentType, extension) = Resolver(formato);

            using var definicion = Ensamblado.GetManifestResourceStream(NombreRecurso(reporte))
                ?? throw new InvalidOperationException($"No se encontró el reporte {reporte}.rdlc embebido.");

            using var report = new LocalReport();

            report.LoadReportDefinition(definicion);

            // El nombre debe coincidir con <DataSet Name="..."> del .rdlc
            foreach (var (nombre, datos) in dataSets)
            {
                report.DataSources.Add(new ReportDataSource(nombre, datos));
            }

            // Cada parámetro debe existir en <ReportParameters> del .rdlc
            report.SetParameters(parametros.Select(p => new ReportParameter(p.Key, p.Value)));

            var contenido = report.Render(renderFormat);

            if (renderFormat == "HTML5")
            {
                contenido = AsegurarDocumentoHtml(contenido);
            }

            return new ReporteArchivo(contenido, contentType, $"{nombreArchivo}.{extension}");
        }

        private static (string RenderFormat, string ContentType, string Extension) Resolver(FormatoReporte formato)
        {
            return formato switch
            {
                FormatoReporte.Vista when SoportaPdf => ("PDF", "application/pdf", "pdf"),
                FormatoReporte.Vista => ("HTML5", "text/html; charset=utf-8", "html"),

                FormatoReporte.Pdf when SoportaPdf => ("PDF", "application/pdf", "pdf"),
                FormatoReporte.Pdf => throw new BusinessRuleException(
                    "La exportación a PDF requiere que la API se ejecute en Windows. " +
                    "Usa la vista previa e imprime, o exporta a Excel o Word."),

                FormatoReporte.Excel => ("EXCELOPENXML",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),

                FormatoReporte.Word => ("WORDOPENXML",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),

                _ => throw new BusinessRuleException("Formato de reporte no soportado.")
            };
        }

        /// <summary>Si el renderizador devuelve un fragmento, lo envuelve en un documento UTF-8.</summary>
        private static byte[] AsegurarDocumentoHtml(byte[] contenido)
        {
            var html = Encoding.UTF8.GetString(contenido);

            if (html.Contains("<html", StringComparison.OrdinalIgnoreCase))
                return contenido;

            var documento =
                "<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">" +
                "<title>Reporte</title></head><body>" + html + "</body></html>";

            return Encoding.UTF8.GetBytes(documento);
        }

        private static string NombreRecurso(string reporte) => $"Reportes.{reporte}.rdlc";
    }
}
