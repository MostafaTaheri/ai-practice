using AIPractice.Application.Exceptions;
using AIPractice.Application.Features.Standards.Search;
using AIPractice.BuildingBlock;

namespace AIPractice.UnitTests;

public class SearchStandardsServiceTests
{
    private sealed class Embeddings(string model) : IEmbeddingClient
    {
        public string Model => model;
        public Task<float[][]> EmbedAsync(string[] input, CancellationToken cancellationToken) =>
            Task.FromResult(new[] { new[] { 1f } });
    }

    private sealed class Index(string model) : IStandardsIndex
    {
        public string EmbeddingModel => model;
        public IReadOnlyList<SourceHit> Search(float[] queryVector) =>
            [new SourceHit(new StandardSource("STD-001", "T", "Text", "REV"), 0.9)];
    }

    [Fact]
    public async Task Search_ReturnsHitsAndModel()
    {
        var service = new SearchStandardsService(new Embeddings("m"), new Index("m"));

        var result = await service.SearchAsync(new SearchStandardsCommand { Question = "Why?" });

        Assert.Equal("m", result.EmbeddingModel);
        Assert.Equal("STD-001", Assert.Single(result.RetrievedSources).Source.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithBlankQuestion_Throws(string question)
    {
        var service = new SearchStandardsService(new Embeddings("m"), new Index("m"));

        await Assert.ThrowsAsync<AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException.ProposalValidationException>(
            () => service.SearchAsync(new SearchStandardsCommand { Question = question }));
    }

    [Fact]
    public async Task Search_WithOverlongQuestion_Throws()
    {
        var service = new SearchStandardsService(new Embeddings("m"), new Index("m"));

        await Assert.ThrowsAsync<AIPractice.Application.Features.Proposals.Exceptions.BaseBusinessProposalException.ProposalValidationException>(
            () => service.SearchAsync(new SearchStandardsCommand { Question = new string('x', 201) }));
    }

    [Fact]
    public async Task Search_WithModelMismatch_Throws()
    {
        var service = new SearchStandardsService(new Embeddings("a"), new Index("b"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.SearchAsync(new SearchStandardsCommand { Question = "Why?" }));
    }
}
