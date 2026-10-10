using Api.RateLimiting;
using Application.Clientes.Commands;
using Application.Interfaces;
using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [EnableRateLimiting(RateLimitPolicies.Api)]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IValidator<CrearClienteCommand> _crearValidator;
        private readonly IValidator<ActualizarClienteCommand> _actualizarValidator;

        public ClientesController(IClienteService clienteService, IValidator<CrearClienteCommand> crearValidator,
            IValidator<ActualizarClienteCommand> actualizarValidator)
        {
            _clienteService = clienteService;
            _crearValidator = crearValidator;
            _actualizarValidator = actualizarValidator;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var clientes = await _clienteService.ObtenerTodosAsync();
            return Ok(clientes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var cliente = await _clienteService.ObtenerPorIdAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CrearClienteCommand command)
        {
            var validation = await _crearValidator.ValidateAsync(command);
            if (!validation.IsValid)
            {
                return BadRequest(new {message = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage))});
            }

            var id = await _clienteService.CrearClienteAsync(command);
            return CreatedAtAction(nameof(Get), new { id }, command);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ActualizarClienteCommand command)
        {
            var validation = await _actualizarValidator.ValidateAsync(command);
            if (!validation.IsValid)
            {
                return BadRequest(new { message = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)) });
            }

            var actualizado = await _clienteService.ActualizarClienteAsync(id, command);
            if(!actualizado)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var eliminado = await _clienteService.EliminarClienteAsync(id);
            if (!eliminado)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
