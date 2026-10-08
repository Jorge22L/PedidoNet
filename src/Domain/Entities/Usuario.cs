using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Rol { get; set; } = "Usuario";
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Correo corportativo; se usa para vincular la cuenta la primera vez
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Claim "oid" de Entra Id. Identificador inmutable del usuario en el tenant
        /// </summary>
        public string? EntraObjectId { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
