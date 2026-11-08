using AIPractice.BuildingBlock;
using AIPractice.Infrastructure.ExternalServices.Clients.Ollama;
using AIPractice.Infrastructure.ExternalServices.Standards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // The review service owns the shared 90-second deadline.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        // Each typed client is bounded by the service-level deadline.
        services.AddHttpClient<IEmbeddingClient, OllamaEmbeddingClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddHttpClient<IGroundedStandardsClient, OllamaGroundedStandardsClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OllamaOptions>>().Value.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddOptions<StandardsOptions>()
            .Bind(configuration.GetSection(StandardsOptions.SectionName));

        services.AddSingleton<StandardsIndexHostedService>();
        services.AddSingleton<IStandardsIndex>(sp => sp.GetRequiredService<StandardsIndexHostedService>());
        services.AddHostedService(sp => sp.GetRequiredService<StandardsIndexHostedService>());

        return services;
    }
}