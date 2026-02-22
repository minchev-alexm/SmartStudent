using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartStudent.Data;
using SmartStudent.Models;
using System.Linq;
using System.Threading.Tasks;
using UAParser;

namespace SmartStudent.Services
{
    public class VisitorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<VisitorLoggingMiddleware> _logger;

        public VisitorLoggingMiddleware(
            RequestDelegate next,
            ILogger<VisitorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            var path = context.Request.Path.Value?.ToLower();

            if (IsPageRequest(path))
            {
                string ip =
                    context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                    context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                    context.Connection.RemoteIpAddress?.ToString();

                string userAgent = context.Request.Headers["User-Agent"].ToString();

                var parser = Parser.GetDefault();
                var client = parser.Parse(userAgent);

                var log = new VisitorLog
                {
                    IP = ip,
                    Browser = client.UA.Family,
                    OS = client.OS.Family,
                    Device = client.Device.Family,
                    Path = context.Request.Path,
                    VisitTime = DateTime.UtcNow
                };

                db.VisitorLogs.Add(log);
                await db.SaveChangesAsync();

                _logger.LogInformation(
                    "Visit saved | IP: {IP} | {Device} | {Browser} | {Path}",
                    ip, log.Device, log.Browser, path);
            }

            await _next(context);
        }

        private bool IsPageRequest(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains(".")) return false;

            if (path.StartsWith("/lib") ||
                path.StartsWith("/css") ||
                path.StartsWith("/js"))
                return false;

            return true;
        }
    }
}