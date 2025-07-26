using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PostgresControlPlane.Api.Configuration;
using PostgresControlPlane.Api.Models.Contracts;

namespace PostgresControlPlane.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize(Policy = AuthorizationPolicies.FilesManage)]
public sealed partial class FilesController : ControllerBase
{
    private const int MaximumPageSize = 100;
    private const int MaximumOfficeArchiveEntries = 10_000;
    private static readonly Dictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv"
        };

    private readonly StorageOptions _options;
    private readonly string _rootPath;

    public FilesController(IOptions<StorageOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.RootPath));
    }

    [HttpPost]
    public async Task<ActionResult<StoredFileResponse>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > _options.MaxFileSizeBytes)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The file is empty or exceeds the configured size limit"
            });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedContentTypes.TryGetValue(extension, out var contentType) ||
            !HasAllowedDeclaredContentType(file.ContentType, contentType) ||
            !await HasExpectedSignature(file, extension, cancellationToken))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The file type is not allowed or does not match its content"
            });
        }

        Directory.CreateDirectory(_rootPath);
        var fileId = $"{Guid.NewGuid():N}{extension}";
        var filePath = GetSafePath(fileId);

        await using (var destination = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            await file.CopyToAsync(destination, cancellationToken);
        }

        var response = new StoredFileResponse(
            fileId,
            file.Length,
            contentType,
            DateTime.UtcNow,
            Url.ActionLink(nameof(Download), values: new { fileId }) ?? $"api/files/{fileId}");

        return CreatedAtAction(nameof(Download), new { fileId }, response);
    }

    [HttpGet]
    public ActionResult<StoredFilePageResponse> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1 ||
            pageSize is < 1 or > MaximumPageSize ||
            page - 1 > int.MaxValue / Math.Max(pageSize, 1))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pagination",
                detail: $"Page must be at least 1 and page size must be between 1 and {MaximumPageSize}.");
        }

        if (!Directory.Exists(_rootPath))
        {
            return Ok(new StoredFilePageResponse(
                page,
                pageSize,
                0,
                Array.Empty<StoredFileResponse>()));
        }

        var files = Directory.EnumerateFiles(_rootPath)
            .Select(path => new FileInfo(path))
            .Where(file => FileIdPattern().IsMatch(file.Name))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ToArray();
        var offset = checked((page - 1) * pageSize);
        var items = files
            .Skip(offset)
            .Take(pageSize)
            .Select(file => new StoredFileResponse(
                file.Name,
                file.Length,
                GetContentType(file.Extension),
                file.CreationTimeUtc,
                Url.ActionLink(nameof(Download), values: new { fileId = file.Name }) ?? $"api/files/{file.Name}"))
            .ToArray();

        return Ok(new StoredFilePageResponse(page, pageSize, files.Length, items));
    }

    [HttpGet("{fileId}")]
    public IActionResult Download(string fileId)
    {
        if (!FileIdPattern().IsMatch(fileId))
        {
            return NotFound();
        }

        var filePath = GetSafePath(fileId);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);
        Response.Headers["Content-Security-Policy"] = "sandbox";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(
            stream,
            GetContentType(Path.GetExtension(fileId)),
            fileId,
            enableRangeProcessing: true);
    }

    [HttpDelete("{fileId}")]
    public IActionResult Delete(string fileId)
    {
        if (!FileIdPattern().IsMatch(fileId))
        {
            return NotFound();
        }

        var filePath = GetSafePath(fileId);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        System.IO.File.Delete(filePath);
        return NoContent();
    }

    private string GetSafePath(string fileId)
    {
        var path = Path.GetFullPath(Path.Combine(_rootPath, fileId));
        if (!path.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The resolved file path escaped the storage root.");
        }

        return path;
    }

    private static string GetContentType(string extension)
    {
        return AllowedContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static bool HasAllowedDeclaredContentType(string declaredContentType, string expectedContentType)
    {
        if (string.IsNullOrWhiteSpace(declaredContentType))
        {
            return true;
        }

        var mediaType = declaredContentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, expectedContentType, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> HasExpectedSignature(
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header, cancellationToken);

        return extension switch
        {
            ".pdf" => HasPrefix(header, bytesRead, [0x25, 0x50, 0x44, 0x46]),
            ".png" => HasPrefix(header, bytesRead, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            ".jpg" or ".jpeg" => HasPrefix(header, bytesRead, [0xFF, 0xD8, 0xFF]),
            ".docx" or ".xlsx" =>
                HasPrefix(header, bytesRead, [0x50, 0x4B, 0x03, 0x04]) &&
                HasExpectedOfficePackage(stream, extension),
            ".txt" or ".csv" => await IsValidUtf8Text(stream, cancellationToken),
            _ => false
        };
    }

    private static async Task<bool> IsValidUtf8Text(
        Stream stream,
        CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var decoder = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true).GetDecoder();
        var bytes = new byte[8192];
        var characters = new char[8192];

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(bytes, cancellationToken)) > 0)
            {
                if (Array.IndexOf(bytes, (byte)0, 0, bytesRead) >= 0)
                {
                    return false;
                }

                _ = decoder.GetChars(bytes, 0, bytesRead, characters, 0, flush: false);
            }

            _ = decoder.GetChars([], 0, 0, characters, 0, flush: true);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool HasExpectedOfficePackage(Stream stream, string extension)
    {
        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count is 0 or > MaximumOfficeArchiveEntries)
            {
                return false;
            }

            var entryNames = archive.Entries
                .Select(entry => entry.FullName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return entryNames.Contains("[Content_Types].xml") && extension switch
            {
                ".docx" => entryNames.Contains("word/document.xml"),
                ".xlsx" => entryNames.Contains("xl/workbook.xml"),
                _ => false
            };
        }
        catch (InvalidDataException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool HasPrefix(byte[] value, int valueLength, byte[] prefix)
    {
        return valueLength >= prefix.Length &&
               value.AsSpan(0, prefix.Length).SequenceEqual(prefix);
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.(pdf|docx|xlsx|png|jpe?g|txt|csv)$", RegexOptions.IgnoreCase)]
    private static partial Regex FileIdPattern();
}
