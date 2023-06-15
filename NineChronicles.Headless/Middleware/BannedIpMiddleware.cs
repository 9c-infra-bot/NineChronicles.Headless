using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NineChronicles.Headless.Properties;
using Serilog;
using ILogger = Serilog.ILogger;

namespace NineChronicles.Headless.Middleware
{
    public class BannedIpMiddleware
    {
        private static Dictionary<string, DateTimeOffset> _bannedIps = new(); // maintain a list of banned IPs here
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly IOptions<CustomIpRateLimitProperties> _options;

        public BannedIpMiddleware(RequestDelegate next, IOptions<CustomIpRateLimitProperties> options)
        {
            _next = next;
            _options = options;
            _logger = Log.Logger.ForContext<BannedIpMiddleware>();
        }

        public static void BanIp(string ip)
        {
            if (!_bannedIps.ContainsKey(ip))
            {
                _bannedIps.Add(ip, DateTimeOffset.Now);
            }
        }

        public static void UnbanIp(string ip)
        {
            if (_bannedIps.ContainsKey(ip))
            {
                _bannedIps.Remove(ip);
            }
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            var remoteIp = context.Connection.RemoteIpAddress!.ToString();
            if (_bannedIps.ContainsKey(remoteIp))
            {
                if ((DateTimeOffset.Now - _bannedIps[remoteIp]).Minutes >= _options.Value.IpBanMinute)
                {
                    _logger.Information($"[IP-RATE-LIMITER] Unbanning IP {remoteIp} (1-hour ban is expired).");
                    UnbanIp(remoteIp);
                }
                else
                {
                    _logger.Information($"[IP-RATE-LIMITER] IP {remoteIp} has been banned");
                    var message = "{ \"message\": \"Your Ip has been banned.\" }";
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(message);
                }
            }

            return _next(context);
        }
    }
}
