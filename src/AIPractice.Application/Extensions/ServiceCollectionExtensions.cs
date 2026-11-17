using AIPractice.Application.Features.Proposals.Review;
using AIPractice.Application.Features.Proposals.ReviewWithStandards;
using AIPractice.Application.Features.Proposals.Summarize;
using AIPractice.Application.Features.Standards.Search;
using Microsoft.Extensions.DependencyInjection;

namespace AIPractice.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISummarizeService, SummarizeService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IReviewWithStandardsService, ReviewWithStandardsService>();
        services.AddScoped<ISearchStandardsService, SearchStandardsService>();

        return services;
    }
}