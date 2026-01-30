using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Core.Exceptions;

namespace DomusMercatorisDotnetMVC.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AsyncLogService _logService;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, AsyncLogService logService)
        {
            _next = next;
            _logService = logService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await LogExceptionAsync(context, ex);

                if (IsAjaxRequest(context))
                {
                    var statusCode = StatusCodes.Status500InternalServerError;
                    var message = "An internal error occurred. Please try again later.";

                    switch (ex)
                    {
                        case KeyNotFoundException:
                        case NotFoundException:
                            statusCode = StatusCodes.Status404NotFound;
                            message = ex.Message;
                            break;
                        case UnauthorizedAccessException:
                        case ForbiddenException:
                            statusCode = StatusCodes.Status403Forbidden;
                            message = ex.Message;
                            break;
                        case UnauthorizedException:
                            statusCode = StatusCodes.Status401Unauthorized;
                            message = ex.Message;
                            break;
                        case BadRequestException:
                        case ArgumentException:
                            statusCode = StatusCodes.Status400BadRequest;
                            message = ex.Message;
                            break;
                        case StockInsufficientException:
                            statusCode = StatusCodes.Status409Conflict;
                            message = ex.Message;
                            break;
                        case InvalidOperationException:
                            statusCode = StatusCodes.Status409Conflict;
                            message = ex.Message;
                            break;
                    }

                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new 
                    { 
                        success = false, 
                        message = message,
                        error = ex.Message // Optional: hide in production if needed, but keeping for now as per previous logic
                    };

                    await context.Response.WriteAsJsonAsync(errorResponse);
                    return; // Handled, do not propagate
                }

                throw; // Propagate to UseExceptionHandler for HTML redirect
            }
        }

        private bool IsAjaxRequest(HttpContext context)
        {
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return true;

            if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
                return true;

            return false;
        }

        private async Task LogExceptionAsync(HttpContext context, Exception ex)
        {
            var message = BuildFullErrorMessage(context, ex);
            await _logService.LogAsync(message);
        }

        private string BuildFullErrorMessage(HttpContext context, Exception ex)
        {
            var sb = new StringBuilder();

            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Path: {context.Request.Path}");
            sb.AppendLine($"Method: {context.Request.Method}");
            sb.AppendLine();

            sb.AppendLine($"Exception: {ex.GetType().Name}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine();

            var inner = ex.InnerException;
            while (inner != null)
            {
                sb.AppendLine($"Inner: {inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
            }

            sb.AppendLine();
            sb.AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace ?? "(no stacktrace)");
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}
