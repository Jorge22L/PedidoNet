using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Auth
{
    /// <summary>
    /// Datos del usuario ya validados desde el token de Entra Id
    /// </summary>
    public sealed record EntraUserInfo(string ObjectId, string? Email, string? Nombre)
    {
    }
}
