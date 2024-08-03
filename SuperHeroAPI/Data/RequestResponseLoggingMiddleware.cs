namespace SuperHeroAPI.Data
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
    
            var headers = context.Request.Headers.Select(h => $"{h.Key}: {h.Value}").Aggregate((h1, h2) => $"{h1}; {h2}");
            _logger.LogInformation($"Request Headers: {headers}");

  
            context.Request.EnableBuffering();
            var requestBodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);
            var requestBodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();
            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            _logger.LogInformation($"HttpRequest Information: {context.Request.Method} {context.Request.Path} {requestBodyText}");

            await _next(context);


        }
    }
    }
