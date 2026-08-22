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
using System.Security.Cryptography;
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
                x => x.NombreUsuario == request.NombreUsuario &&
                x.Activo);

            if (usuario == null) {
                return null;
            }

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);

            if(resultado == PasswordVerificationResult.Failed)
            {
                return null;
            }

            var accessToken = GenerarAccessToken(usuario);
            var refreshToken = GenerarRefreshToken();
            var refreshExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");
            var refreshExpiraEn = DateTime.UtcNow.AddDays(refreshExpirationDays);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UsuarioId = usuario.UsuarioId,
                TokenHash = HashToken(refreshToken),
                CreadoEn = DateTime.UtcNow,
                ExpiraEn = refreshExpiraEn
            });

            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                ExpiraEn = accessToken.ExpiraEn,
                RefreshToken = refreshToken,
                RefreshTokenExpiraEn = refreshExpiraEn,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol,
            };
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

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToBase64String(bytes);
        }

        private static string GenerarRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var tokenHash = HashToken(request.RefreshToken);

            var refreshToken = await _context.RefreshTokens
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (refreshToken is null)
            {
                return null;
            }

            if (!refreshToken.EstaActivo)
            {
                return null;
            }

            if (!refreshToken.Usuario.Activo)
            {
                return null;
            }

            // Generar nuevo refresh token
            var nuevoRefreshToken = GenerarRefreshToken();

            var nuevoRefreshTokenHash = HashToken(nuevoRefreshToken);

            // Revocar el refreshToken anterior
            refreshToken.RevocadoEn = DateTime.UtcNow;

            refreshToken.ReemplazadoPorTokenHash = nuevoRefreshTokenHash;

            var refreshTokenExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays");

            var nuevoRefreshTokenExpiraEn = DateTime.UtcNow.AddDays(refreshTokenExpirationDays);

            // Guardar el nuevo refreshToken
            _context.RefreshTokens.Add(
                new RefreshToken
                {
                    UsuarioId = refreshToken.UsuarioId,
                    TokenHash = nuevoRefreshTokenHash,
                    CreadoEn = DateTime.UtcNow,
                    ExpiraEn = nuevoRefreshTokenExpiraEn
                });

            // Generar nuevo access token
            var accessToken = GenerarAccessToken(refreshToken.Usuario);

            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken = accessToken.Token,
                ExpiraEn = accessToken.ExpiraEn,
                RefreshToken = nuevoRefreshToken,
                RefreshTokenExpiraEn = nuevoRefreshTokenExpiraEn,
                NombreUsuario = refreshToken.Usuario.NombreUsuario,
                Rol = refreshToken.Usuario.Rol
            };
        }

        private (string Token, DateTime ExpiraEn) GenerarAccessToken(Usuario usuario)
        {
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no está configurado");

            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no está configurado");

            var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no está configurado");

            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");

            var ahora = DateTime.UtcNow;
            var expiraEn = ahora.AddMinutes(expirationMinutes);

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
                expires: expiraEn,
                signingCredentials: credentials
                );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return (tokenString, expiraEn);
        }
    }
}
