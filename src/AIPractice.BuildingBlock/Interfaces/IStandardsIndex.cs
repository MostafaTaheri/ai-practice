namespace AIPractice.BuildingBlock;

public interface IStandardsIndex
{
    string EmbeddingModel { get; }

    IReadOnlyList<SourceHit> Search(float[] queryVector);
}
