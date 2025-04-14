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
            // Don't log requests to Swagger UI
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            // Get UserId from JWT token
            int? userId = null;
            string? username = null;

            // Try to get the JWT token
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                try
                {
                    // Parse the JWT token
                    var tokenHandler = new JwtSecurityTokenHandler();
                    if (tokenHandler.CanReadToken(token))
                    {
                        var jwtToken = tokenHandler.ReadJwtToken(token);
                        
                        // Try to get username from claims
                        username = jwtToken.Claims.FirstOrDefault(c => c.Type == "username" || c.Type == JwtRegisteredClaimNames.Sub)?.Value;
                        
                        // If still no username, check other common claim types
                        if (string.IsNullOrEmpty(username))
                        {
                            username = jwtToken.Claims.FirstOrDefault(c => 
                                c.Type == ClaimTypes.Name || 
                                c.Type == ClaimTypes.NameIdentifier || 
                                c.Type == "unique_name" || 
                                c.Type == "name")?.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If token parsing fails, log the error but continue
                    Console.WriteLine($"Error parsing JWT token: {ex.Message}");
                }
            }

            // If no username from token, try from identity
            if (string.IsNullOrEmpty(username) && context.User.Identity?.IsAuthenticated == true)
            {
                username = context.User.Identity.Name ??
                           context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ??
                           context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }

            // Create log entry
            var log = new RequestLog
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                RequestTime = DateTime.UtcNow
            };

            // Copy the request body
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            try
            {
                await context.Request.Body.CopyToAsync(requestBodyStream);
                requestBodyStream.Seek(0, SeekOrigin.Begin);
                var requestBodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();
                // Limit request body size to prevent DB issues
                log.RequestBody = requestBodyText.Length > 10000 ? requestBodyText.Substring(0, 10000) + "..." : requestBodyText;

                requestBodyStream.Seek(0, SeekOrigin.Begin);
                context.Request.Body = requestBodyStream;

                // Capture the response
                var originalResponseBody = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                var startTime = DateTime.UtcNow;
                await _next(context);
                var endTime = DateTime.UtcNow;

                log.StatusCode = context.Response.StatusCode;
                log.ResponseTime = endTime;
                log.Duration = endTime - startTime;

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBodyStream).ReadToEndAsync();
                // Limit response body size to prevent DB issues
                log.ResponseBody = responseBodyText.Length > 10000 ? responseBodyText.Substring(0, 10000) + "..." : responseBodyText;

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBody);

                // Find user ID from username if available
                if (!string.IsNullOrEmpty(username))
                {
                    using (var scope = context.RequestServices.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                        if (user != null)
                        {
                            log.UserId = user.Id;
                        }
                    }
                }

                // Save log to database
                using (var scope = context.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                    dbContext.RequestLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // If an error occurs during logging, we still want the application to continue
                Console.WriteLine($"Error in request logging middleware: {ex.Message}");
            }
            finally
            {
                // Restore the original request body stream
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