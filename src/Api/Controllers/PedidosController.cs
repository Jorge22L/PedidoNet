using Application.Interfaces;
using Application.Pedidos.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;

        public PedidosController(
            IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        /// <summary>
        /// Obtiene todos los pedidos
        /// </summary>
        [Authorize(Policy = "Pedidos.Read")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var pedidos =
                await _pedidoService.ObtenerTodosAsync();

            return Ok(new
            {
                success = true,
                data = pedidos,
                message = "Pedidos obtenidos correctamente"
            });
        }

        /// <summary>
        /// Obtiene un pedido por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var pedido =
                await _pedidoService.ObtenerPorIdAsync(id);

            if (pedido == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pedido no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                data = pedido,
                message = "Pedido encontrado"
            });
        }

        /// <summary>
        /// Obtiene todos los pedidos de un cliente específico
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetByCliente(
            int clienteId)
        {
            var pedidos =
                await _pedidoService
                    .ObtenerPorClienteAsync(clienteId);

            return Ok(new
            {
                success = true,
                data = pedidos,
                message =
                    $"Pedidos del cliente {clienteId} obtenidos correctamente"
            });
        }

        /// <summary>
        /// Crea un nuevo pedido
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post(
            [FromBody] CrearPedidoCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Datos inválidos",
                    errors = ModelState
                });
            }

            var pedidoId =
                await _pedidoService
                    .CrearPedidoAsync(command);

            return CreatedAtAction(
                nameof(Get),
                new { id = pedidoId },
                new
                {
                    success = true,
                    data = new
                    {
                        id = pedidoId
                    },
                    message = "Pedido creado exitosamente"
                });
        }

        /// <summary>
        /// Actualiza un pedido existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(
            int id,
            [FromBody] ActualizarPedidoCommand command)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Datos inválidos",
                    errors = ModelState
                });
            }

            var actualizado =
                await _pedidoService
                    .ActualizarPedidoAsync(
                        id,
                        command);

            if (!actualizado)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pedido no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Pedido actualizado correctamente"
            });
        }

        /// <summary>
        /// Elimina un pedido
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado =
                await _pedidoService
                    .EliminarPedidoAsync(id);

            if (!eliminado)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pedido no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Pedido eliminado correctamente"
            });
        }

        /// <summary>
        /// Completa un pedido
        /// </summary>
        [HttpPatch("{id}/completar")]
        public async Task<IActionResult> Completar(
            int id)
        {
            var completado =
                await _pedidoService
                    .CompletarPedidoAsync(id);

            if (!completado)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pedido no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Pedido completado correctamente"
            });
        }

        /// <summary>
        /// Cancela un pedido
        /// </summary>
        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(
            int id)
        {
            var cancelado =
                await _pedidoService
                    .CancelarPedidoAsync(id);

            if (!cancelado)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Pedido no encontrado"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Pedido cancelado correctamente"
            });
        }

        /// <summary>
        /// Obtiene estadísticas de pedidos
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<IActionResult> GetEstadisticas()
        {
            var pedidos =
                await _pedidoService.ObtenerTodosAsync();

            var pedidosCompletados =
                pedidos
                    .Where(
                        p => p.Estado == "Completado")
                    .ToList();

            var estadisticas = new
            {
                TotalPedidos =
                    pedidos.Count,

                PedidosPendientes =
                    pedidos.Count(
                        p => p.Estado == "Pendiente"),

                PedidosCompletados =
                    pedidosCompletados.Count,

                PedidosCancelados =
                    pedidos.Count(
                        p => p.Estado == "Cancelado"),

                MontoTotalVentas =
                    pedidosCompletados
                        .Sum(p => p.Total),

                PromedioVentaPorPedido =
                    pedidosCompletados.Count > 0
                        ? pedidosCompletados
                            .Average(p => p.Total)
                        : 0
            };

            return Ok(new
            {
                success = true,
                data = estadisticas,
                message =
                    "Estadísticas obtenidas correctamente"
            });
        }
    }

    public class CambiarEstadoRequest
    {
        public string Estado { get; set; }
            = string.Empty;
    }

}
