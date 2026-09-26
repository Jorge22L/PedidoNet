using Api.RateLimiting;
using Application.Auth;
using Application.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

            if(response is null)
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

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var response = await _authService.RefreshTokenAsync(request);

            if(response is null)
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
