using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class RefreshToken
    {
        public int RefreshTokenId { get; set; }
        public int UsuarioId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreadoEn { get; set; }
        public DateTime ExpiraEn { get; set; }
        public DateTime? RevocadoEn { get; set; }
        public string? ReemplazadoPorTokenHash { get; set; }
        public Usuario Usuario { get; set; } = null;
        public bool EstaActivo => RevocadoEn is null && DateTime.UtcNow < ExpiraEn;
    }
}
