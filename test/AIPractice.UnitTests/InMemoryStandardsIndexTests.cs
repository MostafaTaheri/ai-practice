using AIPractice.BuildingBlock;
using AIPractice.Infrastructure.ExternalServices.Standards;

namespace AIPractice.UnitTests;

public class InMemoryStandardsIndexTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "standards-" + Guid.NewGuid());

    public InMemoryStandardsIndexTests() => Directory.CreateDirectory(_directory);

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    private void WriteStandards(int count)
    {
        for (var i = 1; i <= count; i++)
            File.WriteAllText(Path.Combine(_directory, $"STD-{i:000}.md"), $"# Policy {i}\nBody {i}.");
    }

    [Fact]
    public async Task Build_WithWrongFileCount_Throws()
    {
        WriteStandards(9);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            InMemoryStandardsIndex.BuildAsync(_directory, new FakeEmbeddings(), CancellationToken.None));
    }

    [Fact]
    public async Task Search_ReturnsTopThreeOrderedByScore_WithRevision()
    {
        WriteStandards(10);
        var index = await InMemoryStandardsIndex.BuildAsync(_directory, new FakeEmbeddings(), CancellationToken.None);

        // Document i embeds as a unit vector on axis (i mod 10); query points at axis 4 (STD-004).
        var hits = index.Search(Axis(4));

        Assert.Equal(3, hits.Count);
        Assert.Equal("STD-004", hits[0].Source.Id);
        Assert.Equal(1, hits[0].Score, 6);
        Assert.Equal(64, hits[0].Source.Revision.Length);
        Assert.Equal("fake-embed", index.EmbeddingModel);
    }

    private static float[] Axis(int axis)
    {
        var vector = new float[10];
        vector[axis % 10] = 1f;
        return vector;
    }

    private sealed class FakeEmbeddings : IEmbeddingClient
    {
        public string Model => "fake-embed";

        public Task<float[][]> EmbedAsync(string[] input, CancellationToken cancellationToken) =>
            Task.FromResult(input.Select((_, i) => Axis(i + 1)).ToArray());
    }
}
