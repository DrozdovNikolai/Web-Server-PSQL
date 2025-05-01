using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using PostgreSQL.Data;
using SuperHeroAPI.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;

namespace SuperHeroAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Don't log Swagger requests
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Попытаться достать username из JWT
            string? username = null;
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..].Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    if (handler.CanReadToken(token))
                    {
                        var jwt = handler.ReadJwtToken(token);
                        username = jwt.Claims.FirstOrDefault(c => c.Type == "username" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                        if (string.IsNullOrEmpty(username))
                        {
                            username = jwt.Claims.FirstOrDefault(c =>
                                c.Type == ClaimTypes.Name ||
                                c.Type == ClaimTypes.NameIdentifier ||
                                c.Type == "unique_name" ||
                                c.Type == "name")?.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing JWT token: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(username) && context.User.Identity?.IsAuthenticated == true)
            {
                username = context.User.Identity?.Name
                        ?? context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                        ?? context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }

            // Собираем лог-запись
            var log = new RequestLog
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                RequestTime = DateTime.UtcNow
            };

            // Читаем тело запроса (маскирование пароля при ауте)...
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;
            var isMultipart = context.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;
            var isFileDownload = context.Request.Path.Value?.Contains("/download/", StringComparison.OrdinalIgnoreCase) == true;
            var isAuthRequest = context.Request.Path.Value?.Contains("/auth/", StringComparison.OrdinalIgnoreCase) == true;

            try
            {
                if (isMultipart || isFileDownload)
                {
                    log.RequestBody = "<binary>";
                }
                else
                {
                    await context.Request.Body.CopyToAsync(requestBodyStream);
                    requestBodyStream.Position = 0;
                    var text = await new StreamReader(requestBodyStream).ReadToEndAsync();

                    if (isAuthRequest && !string.IsNullOrEmpty(text))
                    {
                        text = Regex.Replace(text, "\"[Pp]assword\"\\s*:\\s*\"[^\"]*\"", "\"Password\":\"*****\"");
                    }

                    log.RequestBody = text.Length > 10000 ? text[..10000] + "..." : text;

                    requestBodyStream.Position = 0;
                    context.Request.Body = requestBodyStream;
                }

                // Перехватим ответ
                var originalResponseBody = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                var start = DateTime.UtcNow;
                await _next(context);
                var end = DateTime.UtcNow;

                log.StatusCode = context.Response.StatusCode;
                log.ResponseTime = end;
                log.Duration = end - start;

                responseBodyStream.Position = 0;
                var respText = await new StreamReader(responseBodyStream).ReadToEndAsync();
                if (context.Response.ContentType != null &&
                    (context.Response.ContentType.Contains("application/octet-stream") ||
                     context.Response.ContentType.Contains("application/vnd.openxmlformats") ||
                     respText.Length > 1_000_000))
                {
                    log.ResponseBody = "<binary>";
                }
                else
                {
                    log.ResponseBody = respText.Length > 10000 ? respText[..10000] + "..." : respText;
                }

                responseBodyStream.Position = 0;
                await responseBodyStream.CopyToAsync(originalResponseBody);

                // *** ЕДИНСТВЕННЫЙ scope и DataContext ***
                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                // Найти userId, если username есть
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null)
                        log.UserId = user.Id;
                }

                // Сохранить RequestLog
                dbContext.RequestLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in request logging middleware: {ex.Message}");
            }
            finally
            {
                // Восстановить тело запроса
                context.Request.Body = originalRequestBody;
            }
        }
    }

        // Extension method for easy middleware registration
        public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
} 