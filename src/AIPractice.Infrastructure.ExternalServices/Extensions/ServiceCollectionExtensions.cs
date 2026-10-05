using AIPractice.BuildingBlock;
using AIPractice.Infrastructure.ExternalServices.Clients.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AIPractice.Infrastructure.ExternalServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OllamaOptions>()
            .Bind(configuration.GetSection(OllamaOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), $"'{OllamaOptions.SectionName}:BaseUrl' is required.")
            .ValidateOnStart();

        services.AddHttpClient<IArchitectureModelClient, OllamaArchitectureModelClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
        });

        return services;
    }
}