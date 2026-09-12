using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Files
{
    public sealed record ImageValidationResult(
        bool IsValid,
        string? ContentType,
        string? Error)
    {
        public static ImageValidationResult Valid(string contentType)
        {
            return new(true, contentType, null);
        }

        public static ImageValidationResult Invalid(string error)
        {
            return new(false, null, error);
        }
    }
}
