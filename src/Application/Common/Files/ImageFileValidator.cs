using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Files
{
    public static class ImageFileValidator
    {
        private const int HeaderLength = 12;

        public static async Task<ImageValidationResult> ValidateAsync(Stream stream,
            string fileName, string contentType, CancellationToken cancellationToken)
        {
            if(stream is null)
            {
                return ImageValidationResult.Invalid("No se recibió ningún archivo.");
            }

            var extension = Path
                .GetExtension(fileName)
                .ToLowerInvariant();

            if(!TryGetExpectedType(extension, contentType, out var expectedType))
            {
                return ImageValidationResult.Invalid("La extension o el Content-Type no están permitidos");
            }

            var header = new byte[HeaderLength];

            int bytesRead;

            if (stream.CanSeek)
            {
                var originalPosition = stream.Position;

                bytesRead = await stream.ReadAsync(header.AsMemory(0, HeaderLength), cancellationToken);

                stream.Position = originalPosition;
            }
            else
            {
                bytesRead = await stream.ReadAsync(header.AsMemory(0, HeaderLength), cancellationToken);
            }

            if(bytesRead == 0)
            {
                return ImageValidationResult.Invalid("El archivo está vacío");
            }

            var detectedType = DetectImageType(header.AsSpan(0, bytesRead));

            if(detectedType is null)
            {
                return ImageValidationResult.Invalid("La extensión, el Content-Type y el contenido del archivo no coinciden");
            }

            return ImageValidationResult.Valid(detectedType);
        }

        private static string? DetectImageType(ReadOnlySpan<byte> header)
        {
            if (IsJpeg(header))
            {
                return "image/jpeg";
            }
            if (IsPng(header))
            {
                return "image/png";
            }
            if (IsWebP(header))
            {
                return "image/webp";
            }

            return null;

        }

        private static bool IsJpeg(ReadOnlySpan<byte> header)
        {
            return header.Length >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;
        }

        private static bool IsPng(ReadOnlySpan<byte> header)
        {
            ReadOnlySpan<byte> signature = [
                0x89,
                0x50,
                0x4E,
                0x47,
                0x0D,
                0x0A,
                0x1A,
                0x0A
            ];

            return header.Length >= signature.Length && header[..signature.Length]
                .SequenceEqual(signature);
        }

        private static bool IsWebP(ReadOnlySpan<byte> header)
        {
            if (header.Length < 12) return false;

            return header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'E' &&
                header[10] == (byte)'B' &&
                header[11] == (byte)'P';
        }

        private static bool TryGetExpectedType(string extension, string contentType, out string expectedType)
        {
            expectedType = string.Empty;

            var normalizedContentType = contentType.ToLowerInvariant();

            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    if (normalizedContentType != "image/jpeg") return false;

                    expectedType = "image/jpeg";
                    return true;

                case ".png":
                    if (normalizedContentType != "image/png") return false;

                    expectedType = "image/png";
                    return true;

                case ".webp":
                    if (normalizedContentType != "image/webp") return false;

                    expectedType = "image/webp";
                    return true;

                default:
                    return false;
            }
        }
    }
}
