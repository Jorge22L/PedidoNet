using Api.Configuration;
using Api.RateLimiting;
using Application.Auth;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);

            if (response is null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Usuario o contraseña incorrectos"
                });
            }

            return Ok(new
            {
                success = true,
                data = response
            });
        }

        /// <summary>
        /// Intercambia un access token de Microsoft Entra Id por la sesión propia de PedidoNet
        /// </summary>
        /// 
        [Authorize(AuthenticationSchemes = AuthSchemes.EntraId)]
        [HttpPost("entra")]
        public async Task<IActionResult> LoginEntra([FromServices] IConfiguration configuration)
        {
            var requiredScope = configuration["EntraId:RequiredScope"] ?? "access_as_user";

            var scopes = User.FindFirstValue("scp")?
                .Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];

            if (!scopes.Contains(requiredScope))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    success = false,
                    message = "El token de Microsoft no tiene el permiso requerido."
                });
            }

            var objectId = User.FindFirstValue("oid");

            if (string.IsNullOrWhiteSpace(objectId))
            {
                return Unauthorized(new { success = false, message = "Token de Microsoft inválido." });
            }

            var email =
                User.FindFirstValue("preferred_username") ??
                User.FindFirstValue("email") ??
                User.FindFirstValue("upn");

            var response = await _authService.LoginEntraAsync(
                new EntraUserInfo(objectId, email, User.FindFirstValue("name")));

            if (response is null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Tu cuenta de Microsoft no está habilitada en PedidoNet."
                });
            }

            return Ok(new
            {
                success = true,
                data = response
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);

            if (response is null)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Refresh token no válido o expirado"
                });
            }

            return Ok(new
            {
                success = true,
                data = response
            });
        }

        [AllowAnonymous]
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke(RefreshTokenRequest request)
        {
            var result = await _authService.RevokeTokenAsync(request);

            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
    }
}
