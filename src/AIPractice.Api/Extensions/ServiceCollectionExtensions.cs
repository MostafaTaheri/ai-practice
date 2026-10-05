using System.Text.Json.Serialization;
using Asp.Versioning;
using AIPractice.Api.Helpers;
using AIPractice.Api.Middlewares;

namespace AIPractice.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options => { options.Filters.Add<ActionModelValidationFilter>(); })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonEnumMemberStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        
        services.AddVersioning();
        services.AddOpenTelemetryObservability(configuration);
        services.AddHealthChecks();
        
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddScoped<ExceptionsMiddleware>();
            
        return services;
    }

    public static IServiceCollection AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version")
            );
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        
        return services;
    }
}