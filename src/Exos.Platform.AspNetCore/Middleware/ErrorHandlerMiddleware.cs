#pragma warning disable CA1506 // Avoid excessive class coupling
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1502 // Avoid excessive complexity
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Extensions;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Models;
using Exos.Platform.AspNetCore.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Exos.Platform.AspNetCore.Middleware
{
    /// <summary>
    /// Enables support for formatting unhandled exceptions as JSON for a given request.
    /// </summary>
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly PlatformDefaultsOptions _platformDefaults;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        private readonly ErrorHandlerMiddlewareOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate next request.</param>
        /// <param name="serviceProvider">An <see cref="IServiceProvider" /> instance.</param>
        /// <param name="options">Configuration options.</param>
        public ErrorHandlerMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ErrorHandlerMiddlewareOptions options = default)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            _platformDefaults = serviceProvider.GetRequiredService<IOptions<PlatformDefaultsOptions>>().Value;
            _logger = serviceProvider.GetRequiredService<ILogger<ErrorHandlerMiddleware>>();
            _options = options; // May be null
        }

        /// <summary>
        /// Call the next delegate/middleware in the pipeline.
        /// Catch any failure and return a formatted error response in JSON format.
        /// </summary>
        /// <param name="context">HttpContext.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    // We're too late to save this response
                    throw;
                }

                if (_options?.ExceptionFilter != null)
                {
                    ex = _options.ExceptionFilter(ex);
                }

                // Normalize standard exceptions
                if (ex is InvalidOperationException && ((InvalidOperationException)ex).Message.StartsWith("No authenticationScheme", StringComparison.InvariantCulture))
                {
                    ex = new UnauthorizedException("No valid authentication provided.", ex);
                }
                else if (ex is ArgumentNullException ane)
                {
                    ex = new BadRequestException(ane.ParamName ?? "Unknown", $"Value is required. {ane.Message}", ane);
                }
                else if (ex is ArgumentOutOfRangeException aoore)
                {
                    ex = new BadRequestException(aoore.ParamName ?? "Unknown", $"Value must be valid range. {aoore.Message}", aoore);
                }
                else if (ex is ArgumentException ae)
                {
                    ex = new BadRequestException(ae.ParamName ?? "Unknown", $"Value must be valid. {ae.Message}", ae);
                }

                // Format a response
                ErrorModel model = new ErrorModel { Timestamp = DateTimeOffset.Now, TrackingId = context.GetTrackingId() };
                if (ex is NotFoundException)
                {
                    context.Response.StatusCode = 404;
                    model.Type = ErrorType.InvalidRequestError;
                    model.Message = ex.Message;
                }
                else if (ex is BadRequestException)
                {
                    // Enumerate the validation errors
                    var @params = new List<ErrorParamModel>();
                    foreach (var kvp in ((BadRequestException)ex).ModelState)
                    {
                        var errors = kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? $"The field '{kvp.Key}' is invalid." : e.ErrorMessage).ToArray();
                        @params.Add(new ErrorParamModel
                        {
                            Name = kvp.Key,
                            Errors = errors.Length > 0 ? errors : null,
                        });
                    }

                    context.Response.StatusCode = 400;
                    model.Type = ErrorType.InvalidRequestError;
                    model.Message = ex.Message;
                    model.Params = @params.ToArray();
                }
                else if (ex is UnauthorizedException)
                {
                    context.Response.StatusCode = 401;
                    model.Type = ErrorType.AuthenticationError;
                    model.Message = ex.Message;
                }
                else if (ex is ConflictException)
                {
                    context.Response.StatusCode = 409;
                    model.Type = ErrorType.InvalidRequestError;
                    model.Message = ex.Message;
                }
                else
                {
                    context.Response.StatusCode = 500;
                    model.Type = ErrorType.ApiError;
                    model.Message = "An unexpected error occurred.";
                }

                // Set response content type
                context.Response.ContentType = "application/json";

                // Debug help
                if (_environment.IsDevelopment())
                {
                    model.Exception = ex.ToString();
                }

                if (_platformDefaults.NewtonsoftJsonCompatability)
                {
                    // Old
                    var mvcNewtonsoftJsonOptions = _serviceProvider.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>();
                    var json = JsonConvert.SerializeObject(model, mvcNewtonsoftJsonOptions.Value.SerializerSettings);
                    await context.Response.WriteAsync(json).ConfigureAwait(false);
                }
                else
                {
                    // New
                    var jsonOptions = _serviceProvider.GetRequiredService<IOptions<JsonOptions>>();
                    await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, model, jsonOptions.Value.JsonSerializerOptions).ConfigureAwait(false);
                }

                var paramsString = string.Empty;
                if (model?.Params != null && model?.Params.Length > 0)
                {
                    paramsString = $"With Input params -> {System.Text.Json.JsonSerializer.Serialize(model?.Params)}";
                }

                _logger.LogError(
                    ex,
                    "A(n) '{ExceptionType}' exception occurred. Returning '{ResultCode}' status.{Params}",
                    LoggerHelper.SanitizeValue(ex?.GetType()?.Name),
                    LoggerHelper.SanitizeValue(context?.Response?.StatusCode),
                    LoggerHelper.SanitizeValue(paramsString));
            }
        }
    }
}
#pragma warning restore CA1506 // Avoid excessive class coupling
#pragma warning restore CA1062 // Validate arguments of public methods
#pragma warning restore CA1502 // Avoid excessive complexity