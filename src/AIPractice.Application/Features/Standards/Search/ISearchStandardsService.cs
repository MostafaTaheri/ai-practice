namespace AIPractice.Application.Features.Standards.Search;

public interface ISearchStandardsService
{
    Task<SearchStandardsResult> SearchAsync(SearchStandardsCommand command, CancellationToken cancellationToken = default);
}
