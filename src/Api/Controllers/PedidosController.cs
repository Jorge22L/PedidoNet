using Application.Interfaces;
using Application.Pedidos.Commands;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;

        public PedidosController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        /// <summary>
        /// Obtiene todos los pedidos
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var pedidos = await _pedidoService.ObtenerTodosAsync();
                return Ok(new
                {
                    success = true,
                    data = pedidos,
                    message = "Pedidos obtenidos correctamente"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al obtener pedidos",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene un pedido por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            try
            {
                var pedido = await _pedidoService.ObtenerPorIdAsync(id);
                if (pedido == null)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    data = pedido,
                    message = "Pedido encontrado"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al obtener el pedido",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene todos los pedidos de un cliente específico
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetByCliente(int clienteId)
        {
            try
            {
                var pedidos = await _pedidoService.ObtenerPorClienteAsync(clienteId);
                return Ok(new
                {
                    success = true,
                    data = pedidos,
                    message = $"Pedidos del cliente {clienteId} obtenidos correctamente"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Error al obtener pedidos del cliente",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Crea un nuevo pedido
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CrearPedidoCommand command)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Datos inválidos",
                        errors = ModelState
                    });

                var pedidoId = await _pedidoService.CrearPedidoAsync(command);
                return CreatedAtAction(nameof(Get), new { id = pedidoId }, new
                {
                    success = true,
                    data = new { id = pedidoId },
                    message = "Pedido creado exitosamente"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Actualiza un pedido existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ActualizarPedidoCommand command)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Datos inválidos",
                        errors = ModelState
                    });

                var actualizado = await _pedidoService.ActualizarPedidoAsync(id, command);
                if (!actualizado)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Pedido actualizado correctamente"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Elimina un pedido
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var eliminado = await _pedidoService.EliminarPedidoAsync(id);
                if (!eliminado)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Pedido eliminado correctamente"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Cambia el estado de un pedido
        /// </summary>
        [HttpPatch("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Estado))
                    return BadRequest(new
                    {
                        success = false,
                        message = "El estado es requerido"
                    });

                var actualizado = await _pedidoService.CambiarEstadoPedidoAsync(id, request.Estado);
                if (!actualizado)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    message = $"Estado del pedido cambiado a '{request.Estado}' correctamente"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Completa un pedido
        /// </summary>
        [HttpPatch("{id}/completar")]
        public async Task<IActionResult> Completar(int id)
        {
            try
            {
                var completado = await _pedidoService.CompletarPedidoAsync(id);
                if (!completado)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Pedido completado correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Cancela un pedido
        /// </summary>
        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            try
            {
                var cancelado = await _pedidoService.CancelarPedidoAsync(id);
                if (!cancelado)
                    return NotFound(new
                    {
                        success = false,
                        message = "Pedido no encontrado"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Pedido cancelado correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error interno del servidor",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de pedidos
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<IActionResult> GetEstadisticas()
        {
            try
            {
                var pedidos = await _pedidoService.ObtenerTodosAsync();

                var estadisticas = new
                {
                    TotalPedidos = pedidos.Count,
                    PedidosPendientes = pedidos.Count(p => p.Estado == "Pendiente"),
                    PedidosCompletados = pedidos.Count(p => p.Estado == "Completado"),
                    PedidosCancelados = pedidos.Count(p => p.Estado == "Cancelado"),
                    MontoTotalVentas = pedidos.Where(p => p.Estado == "Completado").Sum(p => p.Total),
                    PromedioVentaPorPedido = pedidos.Where(p => p.Estado == "Completado").Any()
                        ? pedidos.Where(p => p.Estado == "Completado").Average(p => p.Total)
                        : 0
                };

                return Ok(new
                {
                    success = true,
                    data = estadisticas,
                    message = "Estadísticas obtenidas correctamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al obtener estadísticas",
                    error = ex.Message
                });
            }
        }
    }

    public class CambiarEstadoRequest
    {
        public string Estado { get; set; } = string.Empty;
    }

}
