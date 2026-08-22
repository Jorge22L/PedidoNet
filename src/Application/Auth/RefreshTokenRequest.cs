using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Auth
{
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
