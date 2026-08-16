using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Auth
{
    public class LoginRequest
    {
        public string NombreUsusario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
