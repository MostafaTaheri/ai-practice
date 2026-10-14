using AIPractice.Application.Features.Proposals.Review;
using AIPractice.Application.Features.Proposals.Summarize;
using Microsoft.Extensions.DependencyInjection;

namespace AIPractice.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISummarizeService, SummarizeService>();
        services.AddScoped<IReviewService, ReviewService>();

        return services;
    }
}