using Application.Auth;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AuthService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Usuario>();
        }
        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(
                x => x.NombreUsuario == request.NombreUsusario &&
                x.Activo);

            if (usuario == null) {
                return null;
            }

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);

            if(resultado == PasswordVerificationResult.Failed)
            {
                return null;
            }

            return GenerarToken(usuario);
        }

        private LoginResponse GenerarToken(Usuario usuario)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no está configurado");

            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no está configurado");

            var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no está configurado");

            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");

            var ahora = DateTime.UtcNow;
            var expiracion = ahora.AddMinutes(expirationMinutes);

            var claims = new List<Claim>
            {
                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, usuario.NombreUsuario),

                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),

                new(ClaimTypes.Name, usuario.NombreUsuario),

                new(ClaimTypes.Role, usuario.Rol),

                new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: ahora,
                expires: expiracion,
                signingCredentials: credentials
                );

            return new LoginResponse
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiraEn = expiracion,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol
            };
        }
    }
}
