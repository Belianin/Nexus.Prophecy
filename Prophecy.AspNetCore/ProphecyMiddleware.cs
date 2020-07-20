using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nexus.Prophecy.AspNetCore
{
    public class ProphecyMiddleware
    {
        // Авторизация, когда-нибудь
        private readonly RequestDelegate next;

        public ProphecyMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.HasValue)
                return next.Invoke(context);

            return (context.Request.Path.Value, context.Request.Method) switch
            {
                ("/prophecy/status", "GET") => HandlePingRequest(context),
                ("/prophecy/shutdown", "POST") => HandleShutdownRequest(context),
                _ => next.Invoke(context)
            };
        }

        private Task HandlePingRequest(HttpContext context)
        {
            context.Response.StatusCode = 200;
            return context.Response.WriteAsync($"Status is OK", Encoding.UTF8);
        }

        private Task HandleShutdownRequest(HttpContext context)
        {
            // Экспериментальная фигня
            context.Response.StatusCode = 200;
            return context.Response.WriteAsync($"Shutdown after 5 seconds", Encoding.UTF8)
                .ContinueWith(_ => Task.Delay(5 * 1000))
                .ContinueWith(_ => Environment.Exit(0));
        }
    }
}