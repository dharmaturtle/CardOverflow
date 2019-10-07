using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace CardOverflow.Server {
  public class ExceptionLoggingMiddleware {
    private readonly RequestDelegate _next;

    public ExceptionLoggingMiddleware(RequestDelegate next) {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context) {
      try {
        await _next.Invoke(context);
      } catch (Exception e) {
        var user = context?.User?.Identity?.Name ?? "ANONYMOUS";
        Log.ForContext<ExceptionLoggingMiddleware>().Error(e, "An exception occured for {userName} while attempting to access {url}", user, context.Request.Path);
        throw;
      }
    }

  }
}
