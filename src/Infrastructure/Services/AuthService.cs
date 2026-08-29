using Application.Auth;
using Application.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Domain.Repositories;
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
        private readonly IUsuarioRepository _usuarioRepository;

        private readonly IRefreshTokenRepository
            _refreshTokenRepository;

        private readonly IUnitofWork _unitOfWork;

        private readonly IConfiguration _configuration;

        private readonly PasswordHasher<Usuario> _passwordHasher;

        public AuthService(IUsuarioRepository usuarioRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitofWork unitOfWork,
        IConfiguration configuration)
        {
            _usuarioRepository =
            usuarioRepository;

            _refreshTokenRepository =
                refreshTokenRepository;

            _unitOfWork =
                unitOfWork;

            _configuration =
                configuration;

            _passwordHasher =
                new PasswordHasher<Usuario>();
        }


        public async Task<LoginResponse?>
        LoginAsync(LoginRequest request)
        {
            var usuario =
                await _usuarioRepository
                    .ObtenerActivoPorNombreAsync(
                        request.NombreUsuario);

            if (usuario == null)
                return null;

            var resultado =
                _passwordHasher
                    .VerifyHashedPassword(
                        usuario,
                        usuario.PasswordHash,
                        request.Password);

            if (
                resultado ==
                PasswordVerificationResult.Failed)
            {
                return null;
            }

            var accessToken =
                GenerarAccessToken(usuario);

            var refreshToken =
                GenerarRefreshToken();

            var refreshExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            var refreshExpiraEn =
                DateTime.UtcNow.AddDays(
                    refreshExpirationDays);

            var refreshTokenEntity =
                new RefreshToken
                {
                    UsuarioId =
                        usuario.UsuarioId,

                    TokenHash =
                        HashToken(refreshToken),

                    CreadoEn =
                        DateTime.UtcNow,

                    ExpiraEn =
                        refreshExpiraEn
                };

            await _refreshTokenRepository
                .AgregarAsync(
                    refreshTokenEntity);

            await _unitOfWork
                .SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken =
                    accessToken.Token,

                ExpiraEn =
                    accessToken.ExpiraEn,

                RefreshToken =
                    refreshToken,

                RefreshTokenExpiraEn =
                    refreshExpiraEn,

                NombreUsuario =
                    usuario.NombreUsuario,

                Rol =
                    usuario.Rol
            };
        }

        public async Task<LoginResponse?>
            RefreshTokenAsync(
                RefreshTokenRequest request)
        {
            var tokenHash =
                HashToken(
                    request.RefreshToken);

            var refreshToken =
                await _refreshTokenRepository
                    .ObtenerPorHashAsync(
                        tokenHash,
                        incluirUsuario: true);

            if (refreshToken == null)
                return null;

            if (!refreshToken.EstaActivo)
                return null;

            if (!refreshToken.Usuario.Activo)
                return null;

            var nuevoRefreshToken =
                GenerarRefreshToken();

            var nuevoRefreshTokenHash =
                HashToken(
                    nuevoRefreshToken);

            /*
             * Revocar anterior.
             */
            refreshToken.RevocadoEn =
                DateTime.UtcNow;

            refreshToken
                .ReemplazadoPorTokenHash =
                    nuevoRefreshTokenHash;

            var refreshTokenExpirationDays =
                _configuration.GetValue<int>(
                    "Jwt:RefreshTokenExpirationDays");

            var nuevoRefreshTokenExpiraEn =
                DateTime.UtcNow.AddDays(
                    refreshTokenExpirationDays);

            /*
             * Crear nuevo.
             */
            var nuevoToken =
                new RefreshToken
                {
                    UsuarioId =
                        refreshToken.UsuarioId,

                    TokenHash =
                        nuevoRefreshTokenHash,

                    CreadoEn =
                        DateTime.UtcNow,

                    ExpiraEn =
                        nuevoRefreshTokenExpiraEn
                };

            await _refreshTokenRepository
                .AgregarAsync(nuevoToken);

            var accessToken =
                GenerarAccessToken(
                    refreshToken.Usuario);

            await _unitOfWork
                .SaveChangesAsync();

            return new LoginResponse
            {
                AccessToken =
                    accessToken.Token,

                ExpiraEn =
                    accessToken.ExpiraEn,

                RefreshToken =
                    nuevoRefreshToken,

                RefreshTokenExpiraEn =
                    nuevoRefreshTokenExpiraEn,

                NombreUsuario =
                    refreshToken.Usuario
                        .NombreUsuario,

                Rol =
                    refreshToken.Usuario.Rol
            };
        }

        public async Task<bool>
            RevokeTokenAsync(
                RefreshTokenRequest request)
        {
            var hash =
                HashToken(
                    request.RefreshToken);

            var token =
                await _refreshTokenRepository
                    .ObtenerPorHashAsync(hash);

            if (
                token == null ||
                token.RevocadoEn != null)
            {
                return false;
            }

            token.RevocadoEn =
                DateTime.UtcNow;

            await _unitOfWork
                .SaveChangesAsync();

            return true;
        }

        private static string HashToken(
            string token)
        {
            var bytes =
                SHA256.HashData(
                    Encoding.UTF8
                        .GetBytes(token));

            return Convert.ToBase64String(
                bytes);
        }

        private static string
            GenerarRefreshToken()
        {
            var bytes =
                RandomNumberGenerator
                    .GetBytes(64);

            return Convert.ToBase64String(
                bytes);
        }

        private (
            string Token,
            DateTime ExpiraEn)
            GenerarAccessToken(
                Usuario usuario)
        {
            var issuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "Jwt:Issuer no está configurado");

            var audience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "Jwt:Audience no está configurado");

            var key =
                _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key no está configurado");

            var expirationMinutes =
                _configuration.GetValue<int>(
                    "Jwt:ExpirationMinutes");

            var ahora =
                DateTime.UtcNow;

            var expiraEn =
                ahora.AddMinutes(
                    expirationMinutes);

            var claims =
                new List<Claim>
                {
                new(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
                    usuario.NombreUsuario),

                new(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName,
                    usuario.NombreUsuario),

                new(
                    ClaimTypes.Name,
                    usuario.NombreUsuario),

                new(
                    ClaimTypes.Role,
                    usuario.Rol),

                new(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
                };

            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        key));

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms
                        .HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    notBefore: ahora,
                    expires: expiraEn,
                    signingCredentials:
                        credentials);

            var tokenString =
                new JwtSecurityTokenHandler()
                    .WriteToken(token);

            return (
                tokenString,
                expiraEn);
        }

    }
}
